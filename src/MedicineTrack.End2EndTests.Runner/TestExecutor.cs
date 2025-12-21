using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MedicineTrack.End2EndTests.Runner;

/// <summary>
/// Executes E2E tests by reflection, discovering [Fact] and [Theory] attributes
/// and running tests with proper DI integration and OpenTelemetry tracing.
/// </summary>
public class TestExecutor
{
    private readonly ILogger<TestExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivitySource _activitySource;

    // xUnit attribute type names (matched by name to avoid version conflicts)
    private const string FactAttributeName = "Xunit.FactAttribute";
    private const string TheoryAttributeName = "Xunit.TheoryAttribute";
    private const string InlineDataAttributeName = "Xunit.InlineDataAttribute";

    public TestExecutor(
        ILogger<TestExecutor> logger,
        IServiceProvider serviceProvider,
        ActivitySource activitySource)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _activitySource = activitySource;
    }

    public async Task<TestRunResult> RunTestsAsync(CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString("N");
        using var runActivity = _activitySource.StartActivity("E2E Test Suite", ActivityKind.Internal);
        runActivity?.SetTag("test.type", "e2e");
        runActivity?.SetTag("test.framework", "reflection");
        runActivity?.SetTag("test.run.id", runId);

        var results = new ConcurrentBag<TestCaseResult>();
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogInformation("🧪 Starting E2E test run {RunId} at {StartTime}", runId, startTime);

        try
        {
            // Load the test assembly
            var testAssembly = typeof(MedicineTrack.End2EndTests.Tests.MedicationApiTests).Assembly;
            _logger.LogInformation("📦 Loading tests from: {AssemblyPath}", testAssembly.Location);
            runActivity?.SetTag("test.assembly", testAssembly.Location);

            // Discover test classes
            var testClasses = DiscoverTestClasses(testAssembly);
            _logger.LogInformation("🔍 Discovered {Count} test classes", testClasses.Count);

            // Execute each test class
            foreach (var testClass in testClasses)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await ExecuteTestClassAsync(testClass, results, runActivity, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during test execution");
            runActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            runActivity?.AddException(ex);
        }

        var endTime = DateTimeOffset.UtcNow;
        var duration = endTime - startTime;

        var resultList = results.ToList();
        var passed = resultList.Count(r => r.Success);
        var failed = resultList.Count(r => !r.Success && !r.Skipped);
        var skipped = resultList.Count(r => r.Skipped);

        var result = new TestRunResult
        {
            TotalTests = resultList.Count,
            Passed = passed,
            Failed = failed,
            Skipped = skipped,
            Duration = duration,
            FailedTests = resultList.Where(r => !r.Success && !r.Skipped).Select(r => r.TestName).ToList(),
            Success = failed == 0
        };

        // Set final activity tags
        runActivity?.SetTag("test.total", result.TotalTests);
        runActivity?.SetTag("test.passed", result.Passed);
        runActivity?.SetTag("test.failed", result.Failed);
        runActivity?.SetTag("test.skipped", result.Skipped);
        runActivity?.SetTag("test.duration_ms", duration.TotalMilliseconds);
        runActivity?.SetTag("test.success", result.Success);

        if (result.Success)
        {
            runActivity?.SetStatus(ActivityStatusCode.Ok, "All tests passed");
            _logger.LogInformation(
                "✅ Test run completed: {Passed}/{Total} passed in {Duration:F2}s",
                result.Passed, result.TotalTests, duration.TotalSeconds);
        }
        else
        {
            runActivity?.SetStatus(ActivityStatusCode.Error, $"{result.Failed} tests failed");
            _logger.LogError(
                "❌ Test run completed with failures: {Passed}/{Total} passed, {Failed} failed in {Duration:F2}s",
                result.Passed, result.TotalTests, result.Failed, duration.TotalSeconds);

            foreach (var failedTest in result.FailedTests)
            {
                _logger.LogError("  ❌ {TestName}", failedTest);
            }
        }

        return result;
    }

    private List<Type> DiscoverTestClasses(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && HasTestMethods(t))
            .ToList();
    }

    private static bool HasTestMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => IsTestMethod(m));
    }

    private static bool IsTestMethod(MethodInfo method)
    {
        return method.CustomAttributes.Any(attr =>
            attr.AttributeType.FullName == FactAttributeName ||
            attr.AttributeType.FullName == TheoryAttributeName);
    }

    private static bool IsTheoryMethod(MethodInfo method)
    {
        return method.CustomAttributes.Any(attr =>
            attr.AttributeType.FullName == TheoryAttributeName);
    }

    private static List<object?[]> GetInlineData(MethodInfo method)
    {
        var data = new List<object?[]>();

        foreach (var attr in method.CustomAttributes)
        {
            if (attr.AttributeType.FullName == InlineDataAttributeName)
            {
                var args = attr.ConstructorArguments.FirstOrDefault();
                if (args.Value is IReadOnlyCollection<CustomAttributeTypedArgument> typedArgs)
                {
                    data.Add(typedArgs.Select(a => a.Value).ToArray());
                }
            }
        }

        return data;
    }

    private async Task ExecuteTestClassAsync(
        Type testClass,
        ConcurrentBag<TestCaseResult> results,
        Activity? runActivity,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("📋 Executing test class: {ClassName}", testClass.Name);

        object? testInstance = null;

        try
        {
            // Create instance using DI
            testInstance = ActivatorUtilities.CreateInstance(_serviceProvider, testClass);

            // Call InitializeAsync if present
            await InvokeLifecycleMethodAsync(testInstance, "InitializeAsync");

            // Get test methods
            var testMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(IsTestMethod)
                .ToList();

            foreach (var method in testMethods)
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (IsTheoryMethod(method))
                {
                    // Execute Theory with each set of inline data
                    var inlineDataSets = GetInlineData(method);
                    if (inlineDataSets.Count == 0)
                    {
                        // No inline data, skip
                        results.Add(new TestCaseResult(
                            $"{testClass.Name}.{method.Name}",
                            false,
                            TimeSpan.Zero,
                            "Theory has no inline data",
                            true));
                        continue;
                    }

                    foreach (var data in inlineDataSets)
                    {
                        var testName = $"{testClass.Name}.{method.Name}({string.Join(", ", data.Select(d => d?.ToString() ?? "null"))})";
                        var result = await ExecuteTestMethodAsync(testInstance, method, testName, data, runActivity, cancellationToken);
                        results.Add(result);
                    }
                }
                else
                {
                    // Execute Fact
                    var testName = $"{testClass.Name}.{method.Name}";
                    var result = await ExecuteTestMethodAsync(testInstance, method, testName, null, runActivity, cancellationToken);
                    results.Add(result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error setting up test class {ClassName}", testClass.Name);
        }
        finally
        {
            // Call DisposeAsync if present
            if (testInstance != null)
            {
                await InvokeLifecycleMethodAsync(testInstance, "DisposeAsync");

                // Also try IDisposable/IAsyncDisposable
                if (testInstance is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (testInstance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    private async Task InvokeLifecycleMethodAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (method == null || method.GetParameters().Length != 0) return;

        try
        {
            var result = method.Invoke(instance, null);
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask.ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error invoking {MethodName}", methodName);
        }
    }

    private async Task<TestCaseResult> ExecuteTestMethodAsync(
        object testInstance,
        MethodInfo method,
        string testName,
        object?[]? parameters,
        Activity? runActivity,
        CancellationToken cancellationToken)
    {
        using var testActivity = _activitySource.StartActivity(
            $"Test: {testName}",
            ActivityKind.Internal,
            runActivity?.Context ?? default);

        testActivity?.SetTag("test.name", testName);
        testActivity?.SetTag("test.class", testInstance.GetType().Name);
        testActivity?.SetTag("test.method", method.Name);
        testActivity?.SetTag("test.type", "e2e");

        var startTime = Stopwatch.GetTimestamp();

        try
        {
            _logger.LogDebug("▶️ Running: {TestName}", testName);
            testActivity?.AddEvent(new ActivityEvent("test_started"));

            // Invoke the test method
            var result = method.Invoke(testInstance, parameters);

            // Await if async
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask.ConfigureAwait(false);
            }

            var elapsed = Stopwatch.GetElapsedTime(startTime);

            testActivity?.SetTag("test.duration_ms", elapsed.TotalMilliseconds);
            testActivity?.SetTag("test.result", "passed");
            testActivity?.SetStatus(ActivityStatusCode.Ok, "Test passed");
            testActivity?.AddEvent(new ActivityEvent("test_passed"));

            _logger.LogInformation("✅ Passed: {TestName} ({Duration:F2}s)", testName, elapsed.TotalSeconds);

            return new TestCaseResult(testName, true, elapsed);
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTime);
            var ex = tie.InnerException;

            testActivity?.SetTag("test.duration_ms", elapsed.TotalMilliseconds);
            testActivity?.SetTag("test.result", "failed");
            testActivity?.SetTag("test.error.message", ex.Message);
            testActivity?.SetTag("test.error.type", ex.GetType().Name);
            testActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            testActivity?.AddException(ex);
            testActivity?.AddEvent(new ActivityEvent("test_failed"));

            _logger.LogError("❌ Failed: {TestName} ({Duration:F2}s) - {Message}",
                testName, elapsed.TotalSeconds, ex.Message);

            return new TestCaseResult(testName, false, elapsed, ex.Message);
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTime);

            testActivity?.SetTag("test.duration_ms", elapsed.TotalMilliseconds);
            testActivity?.SetTag("test.result", "failed");
            testActivity?.SetTag("test.error.message", ex.Message);
            testActivity?.SetTag("test.error.type", ex.GetType().Name);
            testActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            testActivity?.AddException(ex);
            testActivity?.AddEvent(new ActivityEvent("test_failed"));

            _logger.LogError("❌ Failed: {TestName} ({Duration:F2}s) - {Message}",
                testName, elapsed.TotalSeconds, ex.Message);

            return new TestCaseResult(testName, false, elapsed, ex.Message);
        }
    }

    private record TestCaseResult(
        string TestName,
        bool Success,
        TimeSpan Duration,
        string? ErrorMessage = null,
        bool Skipped = false);
}

public record TestRunResult
{
    public int TotalTests { get; init; }
    public int Passed { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public TimeSpan Duration { get; init; }
    public List<string> FailedTests { get; init; } = new();
    public bool Success { get; init; }
}

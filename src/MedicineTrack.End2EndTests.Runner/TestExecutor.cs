using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

namespace MedicineTrack.End2EndTests.Runner;

/// <summary>
/// Executes xUnit tests programmatically and reports results with full OpenTelemetry tracing.
/// </summary>
public class TestExecutor
{
    private readonly ILogger<TestExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivitySource _activitySource;

    private int _testsRun;
    private int _testsPassed;
    private int _testsFailed;
    private int _testsSkipped;
    private readonly List<string> _failedTests = new();

    // Track individual test activities for proper span lifecycle
    private readonly ConcurrentDictionary<string, Activity?> _testActivities = new();
    private Activity? _runActivity;

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
        // Clear previous state and start root activity for this test run
        _testActivities.Clear();
        _runActivity = _activitySource.StartActivity("E2E Test Suite", ActivityKind.Internal);
        _runActivity?.SetTag("test.type", "e2e");
        _runActivity?.SetTag("test.framework", "xunit");
        _runActivity?.SetTag("test.run.id", Guid.NewGuid().ToString("N"));

        _testsRun = 0;
        _testsPassed = 0;
        _testsFailed = 0;
        _testsSkipped = 0;
        _failedTests.Clear();

        var startTime = DateTimeOffset.UtcNow;
        _logger.LogInformation("🧪 Starting E2E test run at {StartTime}", startTime);

        try
        {
            // Get the test assembly path
            var testAssembly = typeof(MedicineTrack.End2EndTests.Tests.MedicationApiTests).Assembly;
            var assemblyPath = testAssembly.Location;

            _logger.LogInformation("📦 Loading tests from: {AssemblyPath}", assemblyPath);
            _runActivity?.SetTag("test.assembly", assemblyPath);

            using var runner = AssemblyRunner.WithoutAppDomain(assemblyPath);

            // Set up event handlers
            runner.OnDiscoveryComplete = OnDiscoveryComplete;
            runner.OnTestStarting = OnTestStarting;
            runner.OnTestPassed = OnTestPassed;
            runner.OnTestFailed = OnTestFailed;
            runner.OnTestSkipped = OnTestSkipped;
            runner.OnExecutionComplete = OnExecutionComplete;

            // Create a completion source to wait for all tests
            var completionSource = new TaskCompletionSource<bool>();

            runner.OnExecutionComplete = info =>
            {
                OnExecutionComplete(info);
                completionSource.TrySetResult(true);
            };

            // Start the test run
            _runActivity?.AddEvent(new ActivityEvent("tests_started"));
            runner.Start();

            // Wait for completion or cancellation
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(10)); // 10 minute timeout

            try
            {
                await completionSource.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⚠️ Test run was cancelled or timed out");
                _runActivity?.SetStatus(ActivityStatusCode.Error, "Test run cancelled or timed out");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error executing tests");
            _runActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _runActivity?.AddException(ex);
        }

        var endTime = DateTimeOffset.UtcNow;
        var duration = endTime - startTime;

        var result = new TestRunResult
        {
            TotalTests = _testsRun,
            Passed = _testsPassed,
            Failed = _testsFailed,
            Skipped = _testsSkipped,
            Duration = duration,
            FailedTests = _failedTests.ToList(),
            Success = _testsFailed == 0
        };

        // Set activity tags on the run activity
        _runActivity?.SetTag("test.total", result.TotalTests);
        _runActivity?.SetTag("test.passed", result.Passed);
        _runActivity?.SetTag("test.failed", result.Failed);
        _runActivity?.SetTag("test.skipped", result.Skipped);
        _runActivity?.SetTag("test.duration_ms", duration.TotalMilliseconds);
        _runActivity?.SetTag("test.success", result.Success);
        _runActivity?.AddEvent(new ActivityEvent("tests_completed", DateTimeOffset.UtcNow,
            new ActivityTagsCollection
            {
                { "total", result.TotalTests },
                { "passed", result.Passed },
                { "failed", result.Failed }
            }));

        if (result.Success)
        {
            _runActivity?.SetStatus(ActivityStatusCode.Ok, "All tests passed");
            _logger.LogInformation(
                "✅ Test run completed: {Passed}/{Total} passed in {Duration:F2}s",
                result.Passed, result.TotalTests, duration.TotalSeconds);
        }
        else
        {
            _runActivity?.SetStatus(ActivityStatusCode.Error, $"{result.Failed} tests failed");
            _logger.LogError(
                "❌ Test run completed with failures: {Passed}/{Total} passed, {Failed} failed in {Duration:F2}s",
                result.Passed, result.TotalTests, result.Failed, duration.TotalSeconds);

            foreach (var failedTest in result.FailedTests)
            {
                _logger.LogError("  ❌ {TestName}", failedTest);
            }
        }

        // Dispose the run activity
        _runActivity?.Dispose();
        _runActivity = null;

        return result;
    }

    private void OnDiscoveryComplete(DiscoveryCompleteInfo info)
    {
        _logger.LogInformation("🔍 Discovered {Count} test cases", info.TestCasesToRun);
        _runActivity?.SetTag("test.discovered", info.TestCasesToRun);
        _runActivity?.AddEvent(new ActivityEvent("discovery_complete",
            DateTimeOffset.UtcNow,
            new ActivityTagsCollection { { "test_count", info.TestCasesToRun } }));
    }

    private void OnTestStarting(TestStartingInfo info)
    {
        _logger.LogDebug("▶️ Starting: {TestName}", info.TestDisplayName);

        // Create a child span for this specific test
        var testActivity = _activitySource.StartActivity(
            $"Test: {info.TestDisplayName}",
            ActivityKind.Internal,
            _runActivity?.Context ?? default);

        testActivity?.SetTag("test.name", info.TestDisplayName);
        testActivity?.SetTag("test.type", "e2e");
        testActivity?.AddEvent(new ActivityEvent("test_started"));

        _testActivities[info.TestDisplayName] = testActivity;
    }

    private void OnTestPassed(TestPassedInfo info)
    {
        _testsRun++;
        _testsPassed++;

        // Complete the test span with success
        if (_testActivities.TryRemove(info.TestDisplayName, out var testActivity) && testActivity != null)
        {
            testActivity.SetTag("test.duration_ms", info.ExecutionTime * 1000);
            testActivity.SetTag("test.result", "passed");
            testActivity.SetStatus(ActivityStatusCode.Ok, "Test passed");
            testActivity.AddEvent(new ActivityEvent("test_passed",
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection { { "duration_s", info.ExecutionTime } }));
            testActivity.Dispose();
        }

        _logger.LogInformation("✅ Passed: {TestName} ({Duration:F2}s)",
            info.TestDisplayName, info.ExecutionTime);
    }

    private void OnTestFailed(TestFailedInfo info)
    {
        _testsRun++;
        _testsFailed++;
        _failedTests.Add(info.TestDisplayName);

        // Complete the test span with failure
        if (_testActivities.TryRemove(info.TestDisplayName, out var testActivity) && testActivity != null)
        {
            testActivity.SetTag("test.duration_ms", info.ExecutionTime * 1000);
            testActivity.SetTag("test.result", "failed");
            testActivity.SetTag("test.error.message", info.ExceptionMessage);
            testActivity.SetTag("test.error.type", info.ExceptionType);
            testActivity.SetStatus(ActivityStatusCode.Error, info.ExceptionMessage);
            testActivity.AddEvent(new ActivityEvent("test_failed",
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection
                {
                    { "duration_s", info.ExecutionTime },
                    { "exception.type", info.ExceptionType },
                    { "exception.message", info.ExceptionMessage }
                }));

            // Record the exception stack trace if available
            if (!string.IsNullOrEmpty(info.ExceptionStackTrace))
            {
                testActivity.SetTag("test.error.stacktrace", info.ExceptionStackTrace);
            }

            testActivity.Dispose();
        }

        _logger.LogError("❌ Failed: {TestName} ({Duration:F2}s)\n   {Message}",
            info.TestDisplayName, info.ExecutionTime, info.ExceptionMessage);
    }

    private void OnTestSkipped(TestSkippedInfo info)
    {
        _testsRun++;
        _testsSkipped++;

        // Complete the test span as skipped
        if (_testActivities.TryRemove(info.TestDisplayName, out var testActivity) && testActivity != null)
        {
            testActivity.SetTag("test.result", "skipped");
            testActivity.SetTag("test.skip.reason", info.SkipReason);
            testActivity.SetStatus(ActivityStatusCode.Unset, "Test skipped");
            testActivity.AddEvent(new ActivityEvent("test_skipped",
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection { { "reason", info.SkipReason } }));
            testActivity.Dispose();
        }

        _logger.LogWarning("⏭️ Skipped: {TestName} - {Reason}",
            info.TestDisplayName, info.SkipReason);
    }

    private void OnExecutionComplete(ExecutionCompleteInfo info)
    {
        _logger.LogInformation(
            "🏁 Execution complete: {Total} tests, {Failed} failed, {Skipped} skipped in {Duration:F2}s",
            info.TotalTests, info.TestsFailed, info.TestsSkipped, info.ExecutionTime);

        // Clean up any orphaned test activities
        foreach (var kvp in _testActivities)
        {
            kvp.Value?.Dispose();
        }
        _testActivities.Clear();
    }
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

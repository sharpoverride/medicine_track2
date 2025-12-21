using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

namespace MedicineTrack.End2EndTests.Runner;

/// <summary>
/// Executes xUnit tests programmatically and reports results.
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
        using var activity = _activitySource.StartActivity("E2E Test Run", ActivityKind.Internal);
        activity?.SetTag("test.type", "e2e");
        activity?.SetTag("test.framework", "xunit");

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
                activity?.SetStatus(ActivityStatusCode.Error, "Test run cancelled or timed out");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error executing tests");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
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

        // Set activity tags
        activity?.SetTag("test.total", result.TotalTests);
        activity?.SetTag("test.passed", result.Passed);
        activity?.SetTag("test.failed", result.Failed);
        activity?.SetTag("test.skipped", result.Skipped);
        activity?.SetTag("test.duration_ms", duration.TotalMilliseconds);
        activity?.SetTag("test.success", result.Success);

        if (result.Success)
        {
            activity?.SetStatus(ActivityStatusCode.Ok, "All tests passed");
            _logger.LogInformation(
                "✅ Test run completed: {Passed}/{Total} passed in {Duration:F2}s",
                result.Passed, result.TotalTests, duration.TotalSeconds);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, $"{result.Failed} tests failed");
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

    private void OnDiscoveryComplete(DiscoveryCompleteInfo info)
    {
        _logger.LogInformation("🔍 Discovered {Count} test cases", info.TestCasesToRun);
    }

    private void OnTestStarting(TestStartingInfo info)
    {
        _logger.LogDebug("▶️ Starting: {TestName}", info.TestDisplayName);
    }

    private void OnTestPassed(TestPassedInfo info)
    {
        _testsRun++;
        _testsPassed++;
        _logger.LogInformation("✅ Passed: {TestName} ({Duration:F2}s)",
            info.TestDisplayName, info.ExecutionTime);
    }

    private void OnTestFailed(TestFailedInfo info)
    {
        _testsRun++;
        _testsFailed++;
        _failedTests.Add(info.TestDisplayName);
        _logger.LogError("❌ Failed: {TestName} ({Duration:F2}s)\n   {Message}",
            info.TestDisplayName, info.ExecutionTime, info.ExceptionMessage);
    }

    private void OnTestSkipped(TestSkippedInfo info)
    {
        _testsRun++;
        _testsSkipped++;
        _logger.LogWarning("⏭️ Skipped: {TestName} - {Reason}",
            info.TestDisplayName, info.SkipReason);
    }

    private void OnExecutionComplete(ExecutionCompleteInfo info)
    {
        _logger.LogInformation(
            "🏁 Execution complete: {Total} tests, {Failed} failed, {Skipped} skipped in {Duration:F2}s",
            info.TotalTests, info.TestsFailed, info.TestsSkipped, info.ExecutionTime);
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

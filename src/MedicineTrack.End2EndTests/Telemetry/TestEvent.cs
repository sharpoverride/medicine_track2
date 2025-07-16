using System.Text.Json.Serialization;

namespace MedicineTrack.End2EndTests.Telemetry;

public enum TestEventType
{
    TestStarted,
    TestCompleted,
    TestFailed
}

public record TestEvent(
    string TestName,
    TestEventType EventType,
    DateTime Timestamp,
    TimeSpan? Duration = null,
    string? ErrorMessage = null,
    string? StackTrace = null,
    DateTime? NextScheduledRun = null
);

public record TestRunSummary(
    DateTime StartTime,
    DateTime EndTime,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    TimeSpan Duration,
    DateTime NextScheduledRun
);

public interface ITelemetryReporter
{
    Task ReportTestEventAsync(TestEvent testEvent);
    Task ReportTestRunSummaryAsync(TestRunSummary summary);
}

public class ConsoleTelemetryReporter : ITelemetryReporter
{
    private readonly ILogger<ConsoleTelemetryReporter> _logger;

    public ConsoleTelemetryReporter(ILogger<ConsoleTelemetryReporter> logger)
    {
        _logger = logger;
    }

    public Task ReportTestEventAsync(TestEvent testEvent)
    {
        var message = testEvent.EventType switch
        {
            TestEventType.TestStarted => $"🚀 TEST STARTED: {testEvent.TestName} at {testEvent.Timestamp:HH:mm:ss}",
            TestEventType.TestCompleted => $"✅ TEST COMPLETED: {testEvent.TestName} in {testEvent.Duration?.TotalSeconds:F2}s",
            TestEventType.TestFailed => $"❌ TEST FAILED: {testEvent.TestName} - {testEvent.ErrorMessage}",
            _ => $"📊 TEST EVENT: {testEvent.TestName} - {testEvent.EventType}"
        };

        _logger.LogInformation(message);

        if (testEvent.NextScheduledRun.HasValue)
        {
            _logger.LogInformation($"⏰ Next run scheduled for: {testEvent.NextScheduledRun:HH:mm:ss}");
        }

        return Task.CompletedTask;
    }

    public Task ReportTestRunSummaryAsync(TestRunSummary summary)
    {
        _logger.LogInformation($"""
            📈 TEST RUN SUMMARY:
            ├── Duration: {summary.Duration.TotalSeconds:F2}s
            ├── Total Tests: {summary.TotalTests}
            ├── Passed: {summary.PassedTests} ✅
            ├── Failed: {summary.FailedTests} ❌
            └── Next Run: {summary.NextScheduledRun:HH:mm:ss} ⏰
            """);

        return Task.CompletedTask;
    }
}

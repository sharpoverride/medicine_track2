using System.Reflection;
using MedicineTrack.End2EndTests.Runner.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MedicineTrack.End2EndTests.Runner.Scheduling;

public class TestSchedulerOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(10);
    public int? MaxRuns { get; set; } = null;
    public bool RunOnStartup { get; set; } = true;
    public string[] TestFilters { get; set; } = Array.Empty<string>();
}

public class TestScheduler
{
    private readonly ILogger<TestScheduler> _logger;
    private readonly ITelemetryReporter _telemetryReporter;
    private readonly IServiceProvider _serviceProvider;
    private readonly TestSchedulerOptions _options;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Timer? _timer;
    private int _runCount = 0;

    public TestScheduler(
        ILogger<TestScheduler> logger,
        ITelemetryReporter telemetryReporter,
        IServiceProvider serviceProvider,
        TestSchedulerOptions options)
    {
        _logger = logger;
        _telemetryReporter = telemetryReporter;
        _serviceProvider = serviceProvider;
        _options = options;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation($"Starting test scheduler with interval: {_options.Interval}");
        _logger.LogInformation($"Max runs: {_options.MaxRuns?.ToString() ?? "unlimited"}");

        if (_options.RunOnStartup)
        {
            await RunTestsAsync();
        }

        if (!_options.MaxRuns.HasValue || _runCount < _options.MaxRuns.Value)
        {
            _timer = new Timer(async _ => await RunTestsAsync(), null, _options.Interval, _options.Interval);
            _logger.LogInformation($"Scheduled to run every {_options.Interval}");
        }
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping test scheduler...");
        _timer?.Dispose();
        _cancellationTokenSource.Cancel();
        await Task.CompletedTask;
    }

    private async Task RunTestsAsync()
    {
        if (_options.MaxRuns.HasValue && _runCount >= _options.MaxRuns.Value)
        {
            _logger.LogInformation($"Maximum runs ({_options.MaxRuns}) reached. Stopping scheduler.");
            await StopAsync();
            return;
        }

        _runCount++;
        var startTime = DateTime.UtcNow;
        var nextRun = startTime.Add(_options.Interval);

        _logger.LogInformation($"Starting test run #{_runCount} at {startTime:HH:mm:ss}");

        var testResults = await ExecuteTestsAsync(nextRun);

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        var summary = new TestRunSummary(
            startTime,
            endTime,
            testResults.Count,
            testResults.Count(r => r.Success),
            testResults.Count(r => !r.Success),
            duration,
            nextRun
        );

        await _telemetryReporter.ReportTestRunSummaryAsync(summary);
    }

    private async Task<List<TestResult>> ExecuteTestsAsync(DateTime nextRun)
    {
        var results = new List<TestResult>();
        var testClasses = GetTestClasses();

        foreach (var testClass in testClasses)
        {
            var testInstance = ActivatorUtilities.CreateInstance(_serviceProvider, testClass);
            var testMethods = GetTestMethods(testClass);

            // Initialize the test instance if it implements IAsyncLifetime
            if (testInstance is IAsyncLifetime asyncLifetime)
            {
                await asyncLifetime.InitializeAsync();
            }

            try
            {
                foreach (var method in testMethods)
                {
                    var testResult = await ExecuteTestMethodAsync(testInstance, method, nextRun);
                    results.Add(testResult);
                }
            }
            finally
            {
                // Clean up the test instance if it implements IAsyncLifetime
                if (testInstance is IAsyncLifetime asyncLifetimeDispose)
                {
                    await asyncLifetimeDispose.DisposeAsync();
                }
            }
        }

        return results;
    }

    private async Task<TestResult> ExecuteTestMethodAsync(object testInstance, MethodInfo method, DateTime nextRun)
    {
        var testName = $"{testInstance.GetType().Name}.{method.Name}";
        var startTime = DateTime.UtcNow;

        await _telemetryReporter.ReportTestEventAsync(new TestEvent(
            testName,
            TestEventType.TestStarted,
            startTime,
            NextScheduledRun: nextRun
        ));

        try
        {
            var result = method.Invoke(testInstance, null);
            if (result is Task task)
            {
                await task;
            }

            var duration = DateTime.UtcNow - startTime;
            await _telemetryReporter.ReportTestEventAsync(new TestEvent(
                testName,
                TestEventType.TestCompleted,
                DateTime.UtcNow,
                duration
            ));

            return new TestResult(testName, true, duration);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            var innerException = ex.InnerException ?? ex;
            
            await _telemetryReporter.ReportTestEventAsync(new TestEvent(
                testName,
                TestEventType.TestFailed,
                DateTime.UtcNow,
                duration,
                innerException.Message,
                innerException.StackTrace
            ));

            return new TestResult(testName, false, duration, innerException.Message);
        }
    }

    private static List<Type> GetTestClasses()
    {
        // Load the test assembly (MedicineTrack.End2EndTests)
        var testAssembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "MedicineTrack.End2EndTests.dll"));
        
        return testAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && HasTestMethods(t))
            .ToList();
    }

    private static bool HasTestMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => m.GetCustomAttributes(typeof(FactAttribute), true).Any() ||
                      m.GetCustomAttributes(typeof(TheoryAttribute), true).Any());
    }

    private static List<MethodInfo> GetTestMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), true).Any() ||
                        m.GetCustomAttributes(typeof(TheoryAttribute), true).Any())
            .ToList();
    }
}

public record TestResult(
    string TestName,
    bool Success,
    TimeSpan Duration,
    string? ErrorMessage = null
);

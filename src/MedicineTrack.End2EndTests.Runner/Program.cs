using MedicineTrack.End2EndTests.Runner.Scheduling;
using MedicineTrack.End2EndTests.Runner.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;

namespace MedicineTrack.End2EndTests.Runner;

public class Program
{
    // ActivitySource name must match what's registered in OpenTelemetry
    public const string ActivitySourceName = "end2end-tests-runner";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    public static async Task Main(string[] args)
    {
        var options = ParseArgs(args);

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.IncludeScopes = true;
                    options.IncludeFormattedMessage = true;
                    options.ParseStateValues = true;
                    options.AddOtlpExporter();
                });
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Configure OpenTelemetry with proper resource attributes and tracing
                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService(
                            serviceName: "end2end-tests-runner",
                            serviceVersion: "1.0.0",
                            serviceInstanceId: Environment.MachineName))
                    .WithTracing(tracing => tracing
                        .AddSource(ActivitySourceName)  // Register our ActivitySource
                        .AddSource("System.Net.Http")   // Also capture HTTP client activities
                        .AddHttpClientInstrumentation(options =>
                        {
                            options.FilterHttpRequestMessage = (httpRequestMessage) => true;
                            options.RecordException = true;
                            options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                            {
                                activity?.SetTag("http.request.method", httpRequestMessage.Method.ToString());
                                activity?.SetTag("http.url", httpRequestMessage.RequestUri?.ToString());
                            };
                            options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                            {
                                activity?.SetTag("http.response.status_code", (int)httpResponseMessage.StatusCode);
                            };
                        })
                        .AddAspNetCoreInstrumentation()
                        .SetSampler(new AlwaysOnSampler())  // Ensure all traces are sampled
                        .AddOtlpExporter())
                    .WithMetrics(metrics => metrics
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddOtlpExporter());

                // Enrich logs with Activity context so TraceId/SpanId are present in structured logs
                services.Configure<LoggerFactoryOptions>(o =>
                {
                    o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.Baggage | ActivityTrackingOptions.Tags;
                });

                // Add service discovery for Aspire (core + providers)
                services.AddServiceDiscoveryCore();
                // Use configuration-based resolver in dev/test; DNS SRV can be enabled for k8s
                services.AddConfigurationServiceEndpointProvider();
                Console.WriteLine("🔧 [End2EndTests.Runner] Configuring HttpClients with Aspire service discovery");

                // Configure HTTP clients with Aspire service names matching test expectations
                // medicine-track-api
                services.AddHttpClient("medicine-track-api", client =>
                {
                    // Named endpoint "api-http" for the medicine-track-api service
                    client.BaseAddress = new Uri("https+http://_api-http.medicine-track-api");
                })
                .AddServiceDiscovery();

                // medicine-track-config
                services.AddHttpClient("medicine-track-config", client =>
                {
                    // Named endpoint "config-http" for the medicine-track-config service
                    client.BaseAddress = new Uri("https+http://_config-http.medicine-track-config");
                })
                .AddServiceDiscovery();

                // medicine-track-gateway
                services.AddHttpClient("medicine-track-gateway", client =>
                {
                    // Named endpoint "gateway-http" for the gateway service
                    client.BaseAddress = new Uri("https+http://_gateway-http.medicine-track-gateway");
                })
                .AddServiceDiscovery();

                Console.WriteLine("   ✅ medicine-track-api: Configured with service discovery");
                Console.WriteLine("   ✅ medicine-track-config: Configured with service discovery");
                Console.WriteLine("   ✅ medicine-track-gateway: Configured with service discovery");

                // Register services
                services.AddSingleton(options);

                // Add test fixtures and other services from the test assembly
                // services.AddSingleton<Fixtures.SystemUserFixture>();
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
        
        var host = builder.Build();

        // Start the host to ensure OpenTelemetry is initialized
        await host.StartAsync();

        // Add a simple ActivitySource listener for debugging
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => Console.WriteLine($"📍 Activity Started: {activity.DisplayName} - TraceId: {activity.TraceId}"),
            ActivityStopped = activity => Console.WriteLine($"✅ Activity Stopped: {activity.DisplayName} - Duration: {activity.Duration.TotalMilliseconds}ms")
        };
        ActivitySource.AddActivityListener(listener);

        // Verify ActivitySource is working
        using (var testActivity = ActivitySource.StartActivity("Test Activity", ActivityKind.Internal))
        {
            Console.WriteLine($"🔍 ActivitySource test - Activity created: {testActivity != null}");
            if (testActivity != null)
            {
                Console.WriteLine($"   TraceId: {testActivity.TraceId}");
                Console.WriteLine($"   SpanId: {testActivity.SpanId}");
            }
        }

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🚀 Resolving HttpClients for each service...");
        var httpGatewayService = services.GetRequiredService<IHttpClientFactory>().CreateClient("medicine-track-gateway");
        var httpApiService = services.GetRequiredService<IHttpClientFactory>().CreateClient("medicine-track-api");
        var httpConfigService = services.GetRequiredService<IHttpClientFactory>().CreateClient("medicine-track-config");

        logger.LogInformation("🚀 MedicineTrack E2E Runner initialized. Starting 1s health pings for gateway/api/config...");
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        // Periodically ping health endpoints every second
        var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cts.Token))
            {
                _ = PingAsync(httpGatewayService, "/health", "gateway", logger, cts.Token);
                _ = PingAsync(httpGatewayService, "/medicines/health", "gateway->api", logger, cts.Token);
                _ = PingAsync(httpGatewayService, "/configs/health", "gateway->config", logger, cts.Token);

                _ = PingAsync(httpApiService, "/health", "api", logger, cts.Token);
                _ = PingAsync(httpConfigService, "/health", "config", logger, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("🛑 Cancellation requested. Stopping health pings...");
        }

        logger.LogInformation("✅ Runner exited cleanly.");
    }

    private static async Task PingAsync(HttpClient client, string path, string name, ILogger logger, CancellationToken ct)
    {
        // Force create a new trace by setting Activity.Current to null first
        Activity.Current = null;
        
        // Start a new root activity for each ping to ensure it creates a new trace
        using var activity = ActivitySource.StartActivity(
            $"E2E Health Check: {name}", 
            ActivityKind.Client,
            parentContext: default,  // No parent = new root trace
            tags: new Dictionary<string, object?>
            {
                ["test.suite"] = "e2e-health-check",
                ["test.name"] = $"ping_{name}",
                ["test.target"] = name,
                ["test.endpoint"] = path,
                ["test.type"] = "health-check",
                ["service.name"] = "MedicineTrack.End2EndTests.Runner",
                ["otel.status_code"] = "OK"
            }) ?? Activity.Current;
        
        // If still no activity, force create one
        Activity? finalActivity = activity;
        bool createdManually = false;
        if (finalActivity == null)
        {
            // Manually create an activity as a fallback
            finalActivity = new Activity($"E2E Health Check: {name}");
            finalActivity.SetIdFormat(ActivityIdFormat.W3C);
            finalActivity.Start();
            createdManually = true;
            logger.LogDebug("📍 Manually created activity - TraceId: {TraceId}", finalActivity.TraceId.ToString());
        }

        try
        {
            // Add baggage that will be propagated to downstream services
            finalActivity?.AddBaggage("test.runner", "e2e");
            finalActivity?.AddBaggage("test.run.id", Guid.NewGuid().ToString("N"));
            
            var startTime = DateTimeOffset.UtcNow;
            finalActivity?.AddEvent(new ActivityEvent("health_check_started", startTime));
            
            var resp = await client.GetAsync(path, ct);
            
            var endTime = DateTimeOffset.UtcNow;
            var duration = endTime - startTime;
            
            // Set standard HTTP semantic conventions
            finalActivity?.SetTag("http.method", "GET");
            finalActivity?.SetTag("http.url", $"{client.BaseAddress}{path}");
            finalActivity?.SetTag("http.status_code", (int)resp.StatusCode);
            finalActivity?.SetTag("http.response.status_code", (int)resp.StatusCode);
            finalActivity?.SetTag("test.duration_ms", duration.TotalMilliseconds);
            
            if (!resp.IsSuccessStatusCode)
            {
                finalActivity?.SetStatus(ActivityStatusCode.Error, $"Health check failed with status {resp.StatusCode}");
                finalActivity?.AddEvent(new ActivityEvent("health_check_failed", endTime, 
                    new ActivityTagsCollection {{ "status_code", (int)resp.StatusCode }}));
                logger.LogWarning("[{TraceId}] {Name} health check failed: {Status}", 
                    finalActivity?.TraceId.ToString() ?? "no-trace", name, resp.StatusCode);
            }
            else
            {
                finalActivity?.SetStatus(ActivityStatusCode.Ok, "Health check succeeded");
                finalActivity?.AddEvent(new ActivityEvent("health_check_succeeded", endTime));
                logger.LogInformation("[{TraceId}] {Name} health check OK (duration: {Duration:F2}ms)", 
                    finalActivity?.TraceId.ToString() ?? "no-trace", name, duration.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            finalActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            finalActivity?.RecordException(ex);
            finalActivity?.AddEvent(new ActivityEvent("health_check_exception", 
                DateTimeOffset.UtcNow, 
                new ActivityTagsCollection {{ "exception.type", ex.GetType().Name }, { "exception.message", ex.Message }}));
            
            logger.LogWarning(ex, "[{TraceId}] {Name} health ping failed with exception", 
                finalActivity?.TraceId.ToString() ?? "no-trace", name);
        }
        finally
        {
            // If we manually created the activity, we need to stop it
            if (createdManually && finalActivity != null)
            {
                finalActivity.Stop();
                finalActivity.Dispose();
            }
        }
    }

    private static TestSchedulerOptions ParseArgs(string[] args)
    {
        var options = new TestSchedulerOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--runs" when i + 1 < args.Length:
                    if (int.TryParse(args[i + 1], out int runs))
                    {
                        options.MaxRuns = runs;
                    }
                    i++;
                    break;

                case "--interval" when i + 1 < args.Length:
                    if (int.TryParse(args[i + 1], out int minutes))
                    {
                        options.Interval = TimeSpan.FromMinutes(minutes);
                    }
                    i++;
                    break;

                case "--no-startup":
                    options.RunOnStartup = false;
                    break;
            }
        }

        return options;
    }
}

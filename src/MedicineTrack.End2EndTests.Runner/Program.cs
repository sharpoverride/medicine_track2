using MedicineTrack.End2EndTests.Runner.Scheduling;
using MedicineTrack.End2EndTests.Runner.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MedicineTrack.End2EndTests.Runner;

public class Program
{
    public static async Task Main(string[] args)
    {
        var options = ParseArgs(args);

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
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
                services.AddSingleton<TestScheduler>();
                services.AddSingleton<ITelemetryReporter, ConsoleTelemetryReporter>();
                services.AddLogging(cfg => cfg.AddConsole());

                // Add test fixtures and other services from the test assembly
                services.AddSingleton<MedicineTrack.End2EndTests.Fixtures.SystemUserFixture>();
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        var host = builder.Build();

        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var scheduler = services.GetRequiredService<TestScheduler>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🚀 Resolving HttpClients for each service...");
        var httpGatewayService = services.GetRequiredService<IHttpClientFactory>().CreateClient("medicine-track-gateway");
        var httpApiService = services.GetRequiredService<IHttpClientFactory>().CreateClient("medicine-track-api");
        var httpConfigService = services.GetRequiredService<IHttpClientFactory>().CreateClient("medicine-track-config");

        try
        {
            logger.LogInformation("🚀 Starting MedicineTrack End-to-End Test Runner");
            logger.LogInformation($"📊 Interval: {options.Interval}, Max Runs: {options.MaxRuns?.ToString() ?? "unlimited"}");
            // call the health endpoint for each service
            var healthCheckResponse = await httpGatewayService.GetAsync("/health");
            var healthCheckResponseConfig = await httpConfigService.GetAsync("/health");
            var healthCheckResponseApi = await httpApiService.GetAsync("/health");

            // Start test scheduler
            await scheduler.StartAsync();

            Console.WriteLine("\n⏹️  Press any key to stop the scheduler...");
            Console.ReadKey();
        }
        finally
        {
            logger.LogInformation("🛑 Stopping test scheduler...");
            await scheduler.StopAsync();
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

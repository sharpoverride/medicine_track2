using MedicineTrack.End2EndTests.Scheduling;
using MedicineTrack.End2EndTests.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.Extensions.Http.Resilience;

namespace MedicineTrack.End2EndTests;

class Program
{
    static async Task Main(string[] args)
    {
        var options = ParseArgs(args);
        
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Add service discovery for Aspire
                services.AddServiceDiscovery();
                
                // Configure HTTP clients with Aspire service names
                services.AddHttpClient("medicine-track-api")
                    .AddServiceDiscovery()
                    .AddStandardResilienceHandler();
                    
                services.AddHttpClient("medicine-track-config")
                    .AddServiceDiscovery()
                    .AddStandardResilienceHandler();

                // Register services
                services.AddSingleton(options);
                services.AddSingleton<TestScheduler>();
                services.AddSingleton<ITelemetryReporter, ConsoleTelemetryReporter>();
                services.AddLogging(cfg => cfg.AddConsole());

                // Add test fixtures and other services
                services.AddSingleton<Fixtures.SystemUserFixture>();
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

        try
        {
            logger.LogInformation("🚀 Starting MedicineTrack End-to-End Test Runner");
            logger.LogInformation($"📊 Interval: {options.Interval}, Max Runs: {options.MaxRuns?.ToString() ?? "unlimited"}");
            
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

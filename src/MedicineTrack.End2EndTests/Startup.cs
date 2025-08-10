using System;
using MedicineTrack.End2EndTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MedicineTrack.End2EndTests
{
    /// <summary>
    /// Configures services for the E2E test suite. This class is typically discovered
    /// and used by a test host to set up the dependency injection container.
    /// </summary>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup()
        {
            // Build configuration from appsettings.json and environment variables
            // This allows for flexible configuration for different test environments.
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Enable Aspire service discovery for standalone runs (configuration-based resolver)
            services.AddServiceDiscoveryCore();
            services.AddConfigurationServiceEndpointProvider();
            // Ensure the configuration is available to providers
            services.AddSingleton<IConfiguration>(Configuration);

            // 1. Add Logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            // 2. Register Test Fixtures
            services.AddSingleton<SystemUserFixture>();

            // 3. Configure HttpClients using Aspire-style URIs and named endpoints
            Console.WriteLine("⚙️ [End2EndTests.Startup] Configuring HttpClients for standalone testing using service discovery");

            // Client for the Configuration API (named endpoint: config-http)
            services.AddHttpClient("medicine-track-config", client =>
            {
                client.BaseAddress = new Uri("https+http://_config-http.medicine-track-config");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddServiceDiscovery();

            // Client for the Medication API (named endpoint: api-http)
            services.AddHttpClient("medicine-track-api", client =>
            {
                client.BaseAddress = new Uri("https+http://_api-http.medicine-track-api");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddServiceDiscovery();
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MedicineTrack.End2EndTests.Fixtures;

/// <summary>
/// Fixture that provides DI services for xUnit tests.
/// This allows tests to run both in xUnit (Rider/VS) and in the custom E2E Runner.
///
/// When running in xUnit: Builds its own service provider with HttpClients and service discovery.
/// When running in custom runner: Use ConfigureExternalServices() before InitializeAsync().
/// </summary>
public class TestServicesFixture : IAsyncLifetime
{
    private ServiceProvider? _ownedServiceProvider;
    private IHttpClientFactory? _externalHttpClientFactory;
    private ILoggerFactory? _externalLoggerFactory;
    private SystemUserFixture? _systemUserFixture;
    private bool _useExternalServices;

    /// <summary>
    /// Gets the HttpClientFactory.
    /// </summary>
    public IHttpClientFactory HttpClientFactory => _externalHttpClientFactory
        ?? _ownedServiceProvider?.GetRequiredService<IHttpClientFactory>()
        ?? throw new InvalidOperationException("Services not initialized");

    /// <summary>
    /// Gets the SystemUserFixture with initialized test organization and user.
    /// </summary>
    public SystemUserFixture SystemUserFixture => _systemUserFixture
        ?? throw new InvalidOperationException("SystemUserFixture not initialized");

    /// <summary>
    /// Gets a logger for the specified type.
    /// </summary>
    public ILogger<T> GetLogger<T>()
    {
        if (_externalLoggerFactory != null)
        {
            return _externalLoggerFactory.CreateLogger<T>();
        }
        return _ownedServiceProvider?.GetRequiredService<ILogger<T>>()
            ?? throw new InvalidOperationException("Services not initialized");
    }

    /// <summary>
    /// Configures the fixture to use external services from the custom E2E Runner.
    /// Call this BEFORE InitializeAsync() when running in the custom runner.
    /// </summary>
    public void ConfigureExternalServices(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _externalHttpClientFactory = httpClientFactory;
        _externalLoggerFactory = loggerFactory;
        _useExternalServices = true;
    }

    public async ValueTask InitializeAsync()
    {
        if (_useExternalServices)
        {
            // Use external services from custom runner
            var fixtureLogger = _externalLoggerFactory!.CreateLogger<SystemUserFixture>();
            _systemUserFixture = new SystemUserFixture(_externalHttpClientFactory!, fixtureLogger);
            await _systemUserFixture.InitializeAsync();
            return;
        }

        // Build our own service provider (for xUnit/Rider)
        var services = new ServiceCollection();

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add service discovery for Aspire
        services.AddServiceDiscoveryCore();
        services.AddConfigurationServiceEndpointProvider();

        // Configure HttpClients with service discovery
        services.AddHttpClient("medicine-track-config", client =>
        {
            client.BaseAddress = new Uri("https+http://_config-http.medicine-track-config");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddServiceDiscovery();

        services.AddHttpClient("medicine-track-api", client =>
        {
            client.BaseAddress = new Uri("https+http://_api-http.medicine-track-api");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddServiceDiscovery();

        _ownedServiceProvider = services.BuildServiceProvider();

        // Initialize SystemUserFixture
        var httpClientFactory = _ownedServiceProvider.GetRequiredService<IHttpClientFactory>();
        var logger = _ownedServiceProvider.GetRequiredService<ILogger<SystemUserFixture>>();
        _systemUserFixture = new SystemUserFixture(httpClientFactory, logger);

        await _systemUserFixture.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_systemUserFixture != null)
        {
            await _systemUserFixture.DisposeAsync();
        }

        // Only dispose the service provider if we own it
        _ownedServiceProvider?.Dispose();
    }
}

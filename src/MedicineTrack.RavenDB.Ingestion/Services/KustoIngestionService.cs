using MedicineTrack.RavenDB.Ingestion.Models;
using System.Text;
using System.Text.Json;

namespace MedicineTrack.RavenDB.Ingestion.Services;

/// <summary>
/// TEMPORARY: Stub implementation during migration from Kusto to RavenDB
/// Will be replaced with RavenDBIngestionService in RAVEN-3.3
/// </summary>
public class KustoIngestionService : IKustoIngestionService, IDisposable
{
    private readonly ILogger<KustoIngestionService> _logger;

    public KustoIngestionService(IConfiguration configuration, ILogger<KustoIngestionService> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using stub Kusto ingestion service during migration to RavenDB");
    }

    public Task IngestTracesAsync(IEnumerable<TraceData> traces, CancellationToken cancellationToken = default)
    {
        var count = traces.Count();
        _logger.LogInformation("STUB: Would ingest {Count} traces (not implemented yet)", count);
        return Task.CompletedTask;
    }

    public Task IngestRequestsAsync(IEnumerable<RequestData> requests, CancellationToken cancellationToken = default)
    {
        var count = requests.Count();
        _logger.LogInformation("STUB: Would ingest {Count} requests (not implemented yet)", count);
        return Task.CompletedTask;
    }

    public Task IngestDependenciesAsync(IEnumerable<DependencyData> dependencies, CancellationToken cancellationToken = default)
    {
        var count = dependencies.Count();
        _logger.LogInformation("STUB: Would ingest {Count} dependencies (not implemented yet)", count);
        return Task.CompletedTask;
    }

    public Task IngestExceptionsAsync(IEnumerable<ExceptionData> exceptions, CancellationToken cancellationToken = default)
    {
        var count = exceptions.Count();
        _logger.LogInformation("STUB: Would ingest {Count} exceptions (not implemented yet)", count);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // No resources to dispose in stub implementation
    }
}

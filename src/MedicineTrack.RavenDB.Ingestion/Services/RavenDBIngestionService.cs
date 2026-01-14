using MedicineTrack.RavenDB.Ingestion.Models;
using Raven.Client.Documents;

namespace MedicineTrack.RavenDB.Ingestion.Services;

/// <summary>
/// Service for ingesting telemetry data into RavenDB
/// </summary>
public class RavenDBIngestionService : IRavenDBIngestionService
{
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<RavenDBIngestionService> _logger;

    public RavenDBIngestionService(
        IDocumentStore documentStore,
        ILogger<RavenDBIngestionService> logger)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ingest trace documents (logs) into RavenDB Traces collection
    /// </summary>
    public async Task IngestTracesAsync(IEnumerable<TraceDocument> traces, CancellationToken cancellationToken = default)
    {
        var traceList = traces.ToList();
        if (traceList.Count == 0)
        {
            _logger.LogDebug("No traces to ingest");
            return;
        }

        try
        {
            using var session = _documentStore.OpenAsyncSession();

            foreach (var trace in traceList)
            {
                await session.StoreAsync(trace, cancellationToken);
            }

            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully ingested {Count} traces into RavenDB", traceList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest {Count} traces into RavenDB", traceList.Count);
            throw;
        }
    }

    /// <summary>
    /// Ingest request documents (HTTP requests) into RavenDB Requests collection
    /// </summary>
    public async Task IngestRequestsAsync(IEnumerable<RequestDocument> requests, CancellationToken cancellationToken = default)
    {
        var requestList = requests.ToList();
        if (requestList.Count == 0)
        {
            _logger.LogDebug("No requests to ingest");
            return;
        }

        try
        {
            using var session = _documentStore.OpenAsyncSession();

            foreach (var request in requestList)
            {
                await session.StoreAsync(request, cancellationToken);
            }

            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully ingested {Count} requests into RavenDB", requestList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest {Count} requests into RavenDB", requestList.Count);
            throw;
        }
    }

    /// <summary>
    /// Ingest dependency documents (external calls) into RavenDB Dependencies collection
    /// </summary>
    public async Task IngestDependenciesAsync(IEnumerable<DependencyDocument> dependencies, CancellationToken cancellationToken = default)
    {
        var dependencyList = dependencies.ToList();
        if (dependencyList.Count == 0)
        {
            _logger.LogDebug("No dependencies to ingest");
            return;
        }

        try
        {
            using var session = _documentStore.OpenAsyncSession();

            foreach (var dependency in dependencyList)
            {
                await session.StoreAsync(dependency, cancellationToken);
            }

            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully ingested {Count} dependencies into RavenDB", dependencyList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest {Count} dependencies into RavenDB", dependencyList.Count);
            throw;
        }
    }

    /// <summary>
    /// Ingest exception documents into RavenDB Exceptions collection
    /// </summary>
    public async Task IngestExceptionsAsync(IEnumerable<ExceptionDocument> exceptions, CancellationToken cancellationToken = default)
    {
        var exceptionList = exceptions.ToList();
        if (exceptionList.Count == 0)
        {
            _logger.LogDebug("No exceptions to ingest");
            return;
        }

        try
        {
            using var session = _documentStore.OpenAsyncSession();

            foreach (var exception in exceptionList)
            {
                await session.StoreAsync(exception, cancellationToken);
            }

            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully ingested {Count} exceptions into RavenDB", exceptionList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest {Count} exceptions into RavenDB", exceptionList.Count);
            throw;
        }
    }
}

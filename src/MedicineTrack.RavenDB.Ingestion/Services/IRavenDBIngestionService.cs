using MedicineTrack.RavenDB.Ingestion.Models;

namespace MedicineTrack.RavenDB.Ingestion.Services;

/// <summary>
/// Service for ingesting telemetry data into RavenDB
/// </summary>
public interface IRavenDBIngestionService
{
    /// <summary>
    /// Ingest trace documents (logs) into RavenDB Traces collection
    /// </summary>
    Task IngestTracesAsync(IEnumerable<TraceDocument> traces, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest request documents (HTTP requests) into RavenDB Requests collection
    /// </summary>
    Task IngestRequestsAsync(IEnumerable<RequestDocument> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest dependency documents (external calls) into RavenDB Dependencies collection
    /// </summary>
    Task IngestDependenciesAsync(IEnumerable<DependencyDocument> dependencies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest exception documents into RavenDB Exceptions collection
    /// </summary>
    Task IngestExceptionsAsync(IEnumerable<ExceptionDocument> exceptions, CancellationToken cancellationToken = default);
}

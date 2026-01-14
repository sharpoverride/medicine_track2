using MedicineTrack.RavenDB.Ingestion.Models;

namespace MedicineTrack.RavenDB.Ingestion.Services;

/// <summary>
/// Service for ingesting telemetry data into Kusto
/// </summary>
public interface IKustoIngestionService
{
    /// <summary>
    /// Ingest trace data (logs) into Kusto traces table
    /// </summary>
    Task IngestTracesAsync(IEnumerable<TraceData> traces, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest request data (HTTP requests) into Kusto requests table
    /// </summary>
    Task IngestRequestsAsync(IEnumerable<RequestData> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest dependency data (external calls) into Kusto dependencies table
    /// </summary>
    Task IngestDependenciesAsync(IEnumerable<DependencyData> dependencies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest exception data into Kusto exceptions table
    /// </summary>
    Task IngestExceptionsAsync(IEnumerable<ExceptionData> exceptions, CancellationToken cancellationToken = default);
}

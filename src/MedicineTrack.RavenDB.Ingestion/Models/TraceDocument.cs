namespace MedicineTrack.RavenDB.Ingestion.Models;

/// <summary>
/// RavenDB document model for application traces/logs
/// Maps to the Traces collection in RavenDB
/// </summary>
public record TraceDocument
{
    /// <summary>
    /// RavenDB document ID (auto-generated)
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// When the trace was created
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Log message text
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Severity level: 0=Verbose, 1=Info, 2=Warning, 3=Error, 4=Critical
    /// </summary>
    public int SeverityLevel { get; init; }

    /// <summary>
    /// Operation/endpoint name
    /// </summary>
    public string? OperationName { get; init; }

    /// <summary>
    /// TraceId for correlation across services
    /// </summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>
    /// Span ID within the trace
    /// </summary>
    public string? SpanId { get; init; }

    /// <summary>
    /// Parent span ID for building trace hierarchy
    /// </summary>
    public string? ParentSpanId { get; init; }

    /// <summary>
    /// Service name (e.g., "medicine-track-api")
    /// </summary>
    public string CloudRoleName { get; init; } = string.Empty;

    /// <summary>
    /// Service instance identifier
    /// </summary>
    public string? CloudRoleInstance { get; init; }

    /// <summary>
    /// Custom attributes from OpenTelemetry
    /// </summary>
    public Dictionary<string, string>? CustomDimensions { get; init; }

    /// <summary>
    /// Item type discriminator ("trace")
    /// </summary>
    public string ItemType { get; init; } = "trace";
}

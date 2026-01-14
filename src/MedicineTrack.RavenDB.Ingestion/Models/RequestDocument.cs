namespace MedicineTrack.RavenDB.Ingestion.Models;

/// <summary>
/// RavenDB document model for HTTP requests
/// Maps to the Requests collection in RavenDB
/// </summary>
public record RequestDocument
{
    /// <summary>
    /// RavenDB document ID (auto-generated)
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// When the request was received
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Request name/endpoint
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Full request URL
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Whether the request succeeded (true/false)
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// HTTP status code (e.g., "200", "404")
    /// </summary>
    public string? ResultCode { get; init; }

    /// <summary>
    /// Request duration in milliseconds
    /// </summary>
    public double DurationMs { get; init; }

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
    /// Service name
    /// </summary>
    public string CloudRoleName { get; init; } = string.Empty;

    /// <summary>
    /// Service instance identifier
    /// </summary>
    public string? CloudRoleInstance { get; init; }

    /// <summary>
    /// Custom string attributes
    /// </summary>
    public Dictionary<string, string>? CustomDimensions { get; init; }

    /// <summary>
    /// Custom numeric measurements
    /// </summary>
    public Dictionary<string, double>? CustomMeasurements { get; init; }

    /// <summary>
    /// Item type discriminator ("request")
    /// </summary>
    public string ItemType { get; init; } = "request";
}

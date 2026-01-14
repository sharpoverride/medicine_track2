namespace MedicineTrack.RavenDB.Ingestion.Models;

/// <summary>
/// RavenDB document model for external dependencies (database calls, HTTP requests, etc.)
/// Maps to the Dependencies collection in RavenDB
/// </summary>
public record DependencyDocument
{
    /// <summary>
    /// RavenDB document ID (auto-generated)
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// When the dependency call was made
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Dependency name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Dependency type (SQL, HTTP, Redis, etc.)
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Target server/endpoint
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Command/query/URL data
    /// </summary>
    public string? Data { get; init; }

    /// <summary>
    /// Whether the dependency call succeeded
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Result code (e.g., HTTP status, SQL error code)
    /// </summary>
    public string? ResultCode { get; init; }

    /// <summary>
    /// Call duration in milliseconds
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// Operation/endpoint name
    /// </summary>
    public string? OperationName { get; init; }

    /// <summary>
    /// TraceId for correlation
    /// </summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>
    /// Span ID within the trace
    /// </summary>
    public string? SpanId { get; init; }

    /// <summary>
    /// Service making the dependency call
    /// </summary>
    public string CloudRoleName { get; init; } = string.Empty;

    /// <summary>
    /// Service instance identifier
    /// </summary>
    public string? CloudRoleInstance { get; init; }

    /// <summary>
    /// Custom attributes
    /// </summary>
    public Dictionary<string, string>? CustomDimensions { get; init; }

    /// <summary>
    /// Item type discriminator ("dependency")
    /// </summary>
    public string ItemType { get; init; } = "dependency";
}

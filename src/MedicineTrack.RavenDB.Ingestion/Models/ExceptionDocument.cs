namespace MedicineTrack.RavenDB.Ingestion.Models;

/// <summary>
/// RavenDB document model for application exceptions
/// Maps to the Exceptions collection in RavenDB
/// </summary>
public record ExceptionDocument
{
    /// <summary>
    /// RavenDB document ID (auto-generated)
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// When the exception occurred
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Unique problem identifier for grouping similar exceptions
    /// </summary>
    public string ProblemId { get; init; } = string.Empty;

    /// <summary>
    /// Exception type (e.g., "System.NullReferenceException")
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Exception message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Outer (wrapper) exception type
    /// </summary>
    public string? OuterType { get; init; }

    /// <summary>
    /// Outer exception message
    /// </summary>
    public string? OuterMessage { get; init; }

    /// <summary>
    /// Innermost (root cause) exception type
    /// </summary>
    public string? InnermostType { get; init; }

    /// <summary>
    /// Innermost exception message
    /// </summary>
    public string? InnermostMessage { get; init; }

    /// <summary>
    /// Severity level: 0=Verbose, 1=Info, 2=Warning, 3=Error, 4=Critical
    /// </summary>
    public int SeverityLevel { get; init; }

    /// <summary>
    /// Exception stack trace
    /// </summary>
    public string? Stack { get; init; }

    /// <summary>
    /// Operation/endpoint where exception occurred
    /// </summary>
    public string? OperationName { get; init; }

    /// <summary>
    /// TraceId for correlation
    /// </summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>
    /// Service where exception occurred
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
    /// Item type discriminator ("exception")
    /// </summary>
    public string ItemType { get; init; } = "exception";
}

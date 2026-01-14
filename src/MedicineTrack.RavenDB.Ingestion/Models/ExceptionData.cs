namespace MedicineTrack.RavenDB.Ingestion.Models;

/// <summary>
/// Application Insights exception schema for errors
/// </summary>
public record ExceptionData(
    DateTimeOffset Timestamp,
    string ProblemId,
    string Type,
    string Message,
    string? OuterType,
    string? OuterMessage,
    string? InnermostType,
    string? InnermostMessage,
    int SeverityLevel,
    string? Stack,
    string? OperationName,
    string OperationId,
    string CloudRoleName,
    string? CloudRoleInstance,
    Dictionary<string, object?>? CustomDimensions,
    string ItemType
);

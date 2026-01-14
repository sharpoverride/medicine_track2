namespace MedicineTrack.RavenDB.Ingestion.Models;

/// <summary>
/// Application Insights trace schema for logs
/// </summary>
public record TraceData(
    DateTimeOffset Timestamp,
    string Message,
    int SeverityLevel,
    string? OperationName,
    string OperationId,
    string CloudRoleName,
    string? CloudRoleInstance,
    Dictionary<string, object?>? CustomDimensions,
    string ItemType
);

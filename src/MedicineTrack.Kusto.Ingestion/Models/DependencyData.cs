namespace MedicineTrack.Kusto.Ingestion.Models;

/// <summary>
/// Application Insights dependency schema for external calls (database, HTTP, etc.)
/// </summary>
public record DependencyData(
    DateTimeOffset Timestamp,
    string Name,
    string Type,
    string? Target,
    string? Data,
    string Success,
    string? ResultCode,
    double Duration,
    string? OperationName,
    string OperationId,
    string CloudRoleName,
    string? CloudRoleInstance,
    Dictionary<string, object?>? CustomDimensions,
    string ItemType
);

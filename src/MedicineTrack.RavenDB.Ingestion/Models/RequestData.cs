namespace MedicineTrack.RavenDB.Ingestion.Models;

/// <summary>
/// Application Insights request schema for HTTP requests
/// </summary>
public record RequestData(
    DateTimeOffset Timestamp,
    string Name,
    string? Url,
    string Success,
    string? ResultCode,
    double Duration,
    string? OperationName,
    string OperationId,
    string CloudRoleName,
    string? CloudRoleInstance,
    Dictionary<string, object?>? CustomDimensions,
    Dictionary<string, double>? CustomMeasurements,
    string ItemType
);

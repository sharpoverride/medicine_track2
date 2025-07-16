namespace MedicineTrack.Api.Models;

public record MedicationLog(
    Guid Id,
    Guid UserId,
    Guid MedicationId,
    Guid? ScheduleId,
    DateTimeOffset TakenAt,
    LogStatus Status,
    double? QuantityTaken,
    string? Notes,
    DateTimeOffset LoggedAt
);
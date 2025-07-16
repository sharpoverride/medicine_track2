using MedicineTrack.Medication.Data.Models;

namespace MedicineTrack.Api.DTOs;

public record LogMedicationRequest(
    DateTimeOffset TakenAt,
    LogStatus Status,
    Guid? ScheduleId,
    double? QuantityTaken,
    string? Notes
);

public record UpdateMedicationLogRequest(
    DateTimeOffset? TakenAt,
    LogStatus? Status,
    double? QuantityTaken,
    string? Notes
);
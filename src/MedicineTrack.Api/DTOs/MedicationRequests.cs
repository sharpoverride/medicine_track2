using MedicineTrack.Medication.Data.Models;

namespace MedicineTrack.Api.DTOs;

public record CreateMedicationRequest(
    string Name,
    string? GenericName,
    string? BrandName,
    string Strength,
    string Form,
    string? Shape,
    string? Color,
    string? Notes,
    DateOnly StartDate,
    DateOnly? EndDate,
    List<CreateScheduleRequest> Schedules
);

public record CreateScheduleRequest(
    FrequencyType FrequencyType,
    int? Interval,
    List<DayOfWeek>? DaysOfWeek,
    List<TimeOnly> TimesOfDay,
    double? Quantity,
    string? Unit
);

public record UpdateMedicationRequest(
    string? Name,
    string? GenericName,
    string? BrandName,
    string? Strength,
    string? Form,
    string? Shape,
    string? Color,
    string? Notes,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool? IsArchived,
    List<CreateScheduleRequest>? Schedules
);
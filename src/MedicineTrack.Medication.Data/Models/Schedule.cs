namespace MedicineTrack.Medication.Data.Models;

public record Schedule(
    Guid Id,
    FrequencyType FrequencyType,
    int? Interval,
    List<DayOfWeek>? DaysOfWeek,
    List<TimeOnly> TimesOfDay,
    double? Quantity,
    string? Unit
);
namespace MedicineTrack.Api.Models;

public record Medication(
    Guid Id,
    Guid UserId,
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
    bool IsArchived,
    List<Schedule> Schedules,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
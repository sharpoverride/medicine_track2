namespace MedicineTrack.Medication.Data.Models;

public class Medication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? GenericName { get; set; }
    public string? BrandName { get; set; }
    public string Strength { get; set; } = null!;
    public MedicationForm Form { get; set; }
    public string? Shape { get; set; }
    public string? Color { get; set; }
    public string? Notes { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsArchived { get; set; }
    public List<Schedule> Schedules { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

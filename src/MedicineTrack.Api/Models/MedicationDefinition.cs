namespace MedicineTrack.Api.Models;

public record MedicationDefinition(
    string? NdcCode,
    string Name,
    string? GenericName,
    List<string>? BrandNames,
    List<string> AvailableForms,
    List<string> AvailableStrengths,
    string? Manufacturer
);
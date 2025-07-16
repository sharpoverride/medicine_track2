using MedicineTrack.Api.Models;

namespace MedicineTrack.Api.Services;

public interface IMedicationDatabaseService
{
    Task<List<MedicationDefinition>> SearchMedicationsAsync(string query, string? form = null, string? strength = null);
}
namespace MedicineTrack.Api.Services;

using MedicineTrack.Configuration.Data.Models;

public interface IMedicationDatabaseService
{
    Task<List<MedicationDefinition>> SearchMedicationsAsync(string query, string? form = null, string? strength = null);
}
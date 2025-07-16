namespace MedicineTrack.Api.Repositories;

using Medication = MedicineTrack.Medication.Data.Models.Medication;

public interface IMedicationRepository : IRepository<Medication>
{
    Task<List<Medication>> GetByUserIdAsync(Guid userId, string? status = null, string? search = null);
    Task<Medication?> GetByUserIdAndMedicationIdAsync(Guid userId, Guid medicationId);
    Task<bool> ArchiveMedicationAsync(Guid userId, Guid medicationId);
}
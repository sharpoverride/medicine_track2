using MedicineTrack.Api.Models;

namespace MedicineTrack.Api.Repositories;

public interface IMedicationRepository : IRepository<Medication>
{
    Task<List<Medication>> GetByUserIdAsync(Guid userId, string? status = null, string? search = null);
    Task<Medication?> GetByUserIdAndMedicationIdAsync(Guid userId, Guid medicationId);
    Task<bool> ArchiveMedicationAsync(Guid userId, Guid medicationId);
}
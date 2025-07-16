using MedicineTrack.Api.Models;
using MedicineTrack.Api.DTOs;

namespace MedicineTrack.Api.Services;

public interface IMedicationService
{
    Task<Medication> CreateMedicationAsync(Guid userId, CreateMedicationRequest request);
    Task<List<Medication>> GetUserMedicationsAsync(Guid userId, string? status = null, string? search = null);
    Task<Medication?> GetMedicationByIdAsync(Guid userId, Guid medicationId);
    Task<Medication?> UpdateMedicationAsync(Guid userId, Guid medicationId, UpdateMedicationRequest request);
    Task<bool> DeleteMedicationAsync(Guid userId, Guid medicationId);
}
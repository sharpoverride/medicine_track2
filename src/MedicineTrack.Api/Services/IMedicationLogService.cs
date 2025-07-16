using MedicineTrack.Api.Models;
using MedicineTrack.Api.DTOs;

namespace MedicineTrack.Api.Services;

public interface IMedicationLogService
{
    Task<MedicationLog> LogMedicationAsync(Guid userId, Guid medicationId, LogMedicationRequest request);
    Task<List<MedicationLog>> GetUserLogsAsync(Guid userId, Guid? medicationId = null, DateTime? startDate = null, DateTime? endDate = null, LogStatus? status = null);
    Task<List<MedicationLog>> GetMedicationLogsAsync(Guid userId, Guid medicationId, DateTime? startDate = null, DateTime? endDate = null, LogStatus? status = null);
    Task<MedicationLog?> UpdateLogAsync(Guid userId, Guid logId, UpdateMedicationLogRequest request);
    Task<bool> DeleteLogAsync(Guid userId, Guid logId);
}
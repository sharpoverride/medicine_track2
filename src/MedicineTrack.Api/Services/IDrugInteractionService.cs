using MedicineTrack.Api.Models;
using MedicineTrack.Api.DTOs;

namespace MedicineTrack.Api.Services;

public interface IDrugInteractionService
{
    Task<List<MedicationInteractionWarning>> CheckInteractionsAsync(Guid userId, CheckInteractionRequest request);
}
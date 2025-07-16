using MedicineTrack.Api.Models;

namespace MedicineTrack.Api.DTOs;

public record CheckInteractionRequest(
    List<Guid>? MedicationIds,
    MedicationDefinition? NewMedication,
    List<Guid>? ExistingMedicationIds
);
using MedicineTrack.Medication.Data.Models;

namespace MedicineTrack.Api.Models;

public record InteractingMedicationInfo(Guid? MedicationId, string Name);

public record MedicationInteractionWarning(
    List<InteractingMedicationInfo> InteractingMedications,
    InteractionSeverity Severity,
    string Description,
    string Source
);

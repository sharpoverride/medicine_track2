using FluentValidation;
using MedicineTrack.Api.DTOs;

namespace MedicineTrack.Api.Validators;

public class UpdateMedicationLogRequestValidator : AbstractValidator<UpdateMedicationLogRequest>
{
    public UpdateMedicationLogRequestValidator()
    {
        RuleFor(x => x.TakenAt)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(5))
            .When(x => x.TakenAt.HasValue)
            .WithMessage("Taken at timestamp cannot be in the future.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("A valid log status must be specified (TAKEN, SKIPPED, or LOGGED_AS_NEEDED).");

        RuleFor(x => x.QuantityTaken)
            .GreaterThan(0)
            .When(x => x.QuantityTaken.HasValue)
            .WithMessage("Quantity taken must be a positive number.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

using FluentValidation;
using MedicineTrack.Api.DTOs;

namespace MedicineTrack.Api.Validators;

public class LogMedicationRequestValidator : AbstractValidator<LogMedicationRequest>
{
    public LogMedicationRequestValidator()
    {
        RuleFor(x => x.TakenAt)
            .NotEmpty().WithMessage("Taken at timestamp is required.")
            .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(5))
            .WithMessage("Taken at timestamp cannot be in the future.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid log status must be specified (TAKEN, SKIPPED, or LOGGED_AS_NEEDED).");

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

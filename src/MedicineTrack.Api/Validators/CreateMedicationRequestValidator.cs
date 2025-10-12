using FluentValidation;
using MedicineTrack.Api.DTOs;
using System.Text.RegularExpressions;

namespace MedicineTrack.Api.Validators;

public class CreateMedicationRequestValidator : AbstractValidator<CreateMedicationRequest>
{
    public CreateMedicationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Medication name is required.")
            .MaximumLength(100).WithMessage("Medication name cannot exceed 100 characters.");

        RuleFor(x => x.Strength)
            .NotEmpty().WithMessage("Medication strength is required.")
            .Matches(@"^\d+(\.\d+)?\s+\w+$").WithMessage("Strength must be in a valid format (e.g., '10 mg', '250 mcg').")
            .MaximumLength(50).WithMessage("Strength cannot exceed 50 characters.");

        RuleFor(x => x.Form)
            .IsInEnum().WithMessage("A valid medication form must be specified.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(x => x.Schedules)
            .NotEmpty().WithMessage("At least one schedule is required.");

        RuleForEach(x => x.Schedules)
            .SetValidator(new CreateScheduleRequestValidator());
    }
}
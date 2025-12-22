using FluentValidation;
using MedicineTrack.Api.DTOs;
using MedicineTrack.Medication.Data.Models;

namespace MedicineTrack.Api.Validators;

public class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.FrequencyType)
            .IsInEnum().WithMessage("A valid frequency type must be specified.");

        RuleFor(x => x.Interval)
            .NotNull().WithMessage("Interval is required for this frequency type.")
            .GreaterThan(0).WithMessage("Interval must be a positive number.")
            .When(x => x.FrequencyType == FrequencyType.EVERY_X_DAYS ||
                         x.FrequencyType == FrequencyType.EVERY_X_WEEKS ||
                         x.FrequencyType == FrequencyType.EVERY_X_MONTHS);

        RuleFor(x => x.DaysOfWeek)
            .NotEmpty().WithMessage("At least one day of the week must be specified.")
            .When(x => x.FrequencyType == FrequencyType.SPECIFIC_DAYS_OF_WEEK);

        RuleFor(x => x.TimesOfDay)
            .NotEmpty().WithMessage("At least one time of day must be specified.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be a positive number.")
            .When(x => x.Quantity.HasValue);

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit cannot be empty.")
            .MaximumLength(20).WithMessage("Unit cannot exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.Unit));
    }
}
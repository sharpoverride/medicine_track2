using FluentValidation.TestHelper;
using MedicineTrack.Api.DTOs;
using MedicineTrack.Api.Validators;
using MedicineTrack.Medication.Data.Models;
using Xunit;

namespace MedicineTrack.Tests.Validators;

public class CreateMedicationRequestValidatorTests
{
    private readonly CreateMedicationRequestValidator _validator = new();

    private static CreateMedicationRequest CreateValidRequest() => new(
        Name: "Lisinopril",
        GenericName: "Lisinopril",
        BrandName: "Zestril",
        Strength: "10 mg",
        Form: MedicationForm.Tablet,
        Shape: "Round",
        Color: "White",
        Notes: "Take with water",
        StartDate: new DateOnly(2024, 1, 1),
        EndDate: new DateOnly(2025, 1, 1),
        Schedules: new List<CreateScheduleRequest>
        {
            new(
                FrequencyType: FrequencyType.DAILY,
                Interval: null,
                DaysOfWeek: null,
                TimesOfDay: new List<TimeOnly> { new(8, 0) },
                Quantity: 1,
                Unit: "tablet"
            )
        }
    );

    [Fact]
    public void Should_Have_Error_When_Name_Is_Null()
    {
        var request = CreateValidRequest() with { Name = null };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Medication name is required.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Request_Is_Valid()
    {
        var request = CreateValidRequest();
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Strength_Is_Invalid()
    {
        var request = CreateValidRequest() with { Strength = "invalid" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Strength)
            .WithErrorMessage("Strength must be in a valid format (e.g., '10 mg', '250 mcg').");
    }

    [Fact]
    public void Should_Have_Error_When_EndDate_Is_Before_StartDate()
    {
        var request = CreateValidRequest() with { EndDate = new DateOnly(2023, 12, 31) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("End date must be on or after the start date.");
    }

    [Fact]
    public void Should_Have_Error_When_Schedules_Are_Empty()
    {
        var request = CreateValidRequest() with { Schedules = new List<CreateScheduleRequest>() };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Schedules)
            .WithErrorMessage("At least one schedule is required.");
    }
}
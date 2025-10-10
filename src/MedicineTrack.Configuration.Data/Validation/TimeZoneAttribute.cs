using System;
using System.ComponentModel.DataAnnotations;

namespace MedicineTrack.Configuration.Data.Validation;

/// <summary>
/// Validates that a string is a valid IANA timezone ID.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class TimeZoneAttribute : ValidationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeZoneAttribute"/> class.
    /// </summary>
    public TimeZoneAttribute() : base("The {0} field must be a valid IANA timezone.")
    {
    }

    /// <summary>
    /// Determines whether the specified value is a valid timezone.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context information about the validation operation.</param>
    /// <returns>A <see cref="ValidationResult"/> that indicates whether the specified value is valid or not.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string timeZoneId || string.IsNullOrEmpty(timeZoneId))
        {
            // This attribute does not enforce requirement. Use [Required] for that.
            // It also only validates strings.
            return ValidationResult.Success;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return ValidationResult.Success;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
        {
            var memberName = validationContext.MemberName;
            if (memberName == null)
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), new[] { memberName });
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MedicineTrack.Configuration.Data.Models;
using Xunit;

namespace MedicineTrack.Tests.Models;

public class UserModelTests
{
    private User CreateValidUser() => new(
        Guid.NewGuid(),
        "test@example.com",
        "Test User",
        "UTC",
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow
    );

    [Fact]
    public void User_WithValidData_ShouldPassValidation()
    {
        var user = CreateValidUser();
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(user);
        var isValid = Validator.TryValidateObject(user, context, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void User_WithMissingName_ShouldFailValidation(string name)
    {
        var user = new User(
            Guid.NewGuid(),
            "test@example.com",
            name,
            "UTC",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(user);
        var isValid = Validator.TryValidateObject(user, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(User.Name)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void User_WithMissingEmail_ShouldFailValidation(string email)
    {
        var user = new User(
            Guid.NewGuid(),
            email,
            "Test User",
            "UTC",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(user);
        var isValid = Validator.TryValidateObject(user, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(User.Email)));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    public void User_WithInvalidEmailFormat_ShouldFailValidation(string email)
    {
        var user = new User(
            Guid.NewGuid(),
            email,
            "Test User",
            "UTC",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(user);
        var isValid = Validator.TryValidateObject(user, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(User.Email)));
    }

    [Fact]
    public void User_WithInvalidTimezone_ShouldFailValidation()
    {
        var user = new User(
            Guid.NewGuid(),
            "test@example.com",
            "Test User",
            "Invalid/Timezone",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(user);
        var isValid = Validator.TryValidateObject(user, context, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(User.Timezone)));
    }

    [Theory]
    [InlineData("Europe/London")]
    [InlineData("America/New_York")]
    [InlineData("Etc/UTC")]
    public void User_WithValidTimezone_ShouldPassValidation(string timezone)
    {
        var user = new User(
            Guid.NewGuid(),
            "test@example.com",
            "Test User",
            timezone,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(user);
        var isValid = Validator.TryValidateObject(user, context, validationResults, true);

        Assert.True(isValid);
    }
}
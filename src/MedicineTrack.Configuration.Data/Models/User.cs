using System.ComponentModel.DataAnnotations;
using MedicineTrack.Configuration.Data.Validation;

namespace MedicineTrack.Configuration.Data.Models;

public record User
{
    [Required]
    public Guid Id { get; init; }

    [Required]
    [EmailAddress]
    public string Email { get; init; }

    [Required]
    public string Name { get; init; }

    [TimeZone]
    public string Timezone { get; init; }

    [Required]
    public DateTimeOffset CreatedAt { get; init; }

    [Required]
    public DateTimeOffset UpdatedAt { get; init; }

    public User(Guid id, string email, string name, string timezone, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Email = email;
        Name = name;
        Timezone = timezone;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
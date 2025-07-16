using MedicineTrack.Api.Models.Interfaces;

namespace MedicineTrack.Api.Models;

/// <summary>
/// Base class for entities with unique identifiers
/// </summary>
public abstract record BaseEntity : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
}

/// <summary>
/// Base class for entities that require audit tracking
/// </summary>
public abstract record AuditableEntity : BaseEntity, IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a new instance with updated timestamp
    /// </summary>
    public virtual AuditableEntity WithUpdatedTimestamp()
    {
        return this with { UpdatedAt = DateTimeOffset.UtcNow };
    }
}
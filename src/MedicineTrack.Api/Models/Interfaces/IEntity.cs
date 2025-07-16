namespace MedicineTrack.Api.Models.Interfaces;

/// <summary>
/// Base interface for all entities with unique identifiers
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}
using System.ComponentModel.DataAnnotations;

namespace MedicineTrack.Api.Models.ValueObjects;

/// <summary>
/// Value object representing medication dosage information
/// </summary>
public record Dosage
{
    public double Quantity { get; init; }
    
    [Required]
    [StringLength(50)]
    public string Unit { get; init; } = string.Empty;

    public Dosage() { }

    public Dosage(double quantity, string unit)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit cannot be null or empty", nameof(unit));

        Quantity = quantity;
        Unit = unit.Trim();
    }

    public override string ToString() => $"{Quantity} {Unit}";
}
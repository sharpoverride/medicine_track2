using Microsoft.EntityFrameworkCore;
using MedicineTrack.Api.Models;

namespace MedicineTrack.Configuration.Data;

/// <summary>
/// Database context for configuration and reference data
/// </summary>
public class ConfigurationDbContext : DbContext
{
    public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<MedicationDefinition> MedicationDefinitions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Timezone).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure MedicationDefinition entity
        modelBuilder.Entity<MedicationDefinition>(entity =>
        {
            entity.HasKey(e => e.NdcCode);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GenericName).HasMaxLength(200);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            
            // Configure collections as JSON columns
            entity.Property(e => e.BrandNames)
                .HasConversion(
                    v => v != null ? string.Join('|', v) : null,
                    v => !string.IsNullOrEmpty(v) ? v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList() : null);
            
            entity.Property(e => e.AvailableForms)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());
            
            entity.Property(e => e.AvailableStrengths)
                .HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());
        });
    }
}
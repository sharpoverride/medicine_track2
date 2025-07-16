using Microsoft.EntityFrameworkCore;
using MedicineTrack.Medication.Data.Models;

namespace MedicineTrack.Medication.Data;

/// <summary>
/// Database context for medication-related entities
/// </summary>
public class MedicationDbContext : DbContext
{
    public MedicationDbContext(DbContextOptions<MedicationDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Medication> Medications { get; set; } = null!;
    public DbSet<Schedule> Schedules { get; set; } = null!;
    public DbSet<MedicationLog> MedicationLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Medication entity
        modelBuilder.Entity<Models.Medication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GenericName).HasMaxLength(200);
            entity.Property(e => e.BrandName).HasMaxLength(200);
            entity.Property(e => e.Strength).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Form).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Shape).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            // UserId is a foreign key reference to User in ConfigurationDb
            entity.Property(e => e.UserId).IsRequired();

            // Configure owned collection for Schedules
            entity.OwnsMany(e => e.Schedules, schedule =>
            {
                schedule.WithOwner().HasForeignKey("MedicationId");
                schedule.Property(s => s.Id);
                schedule.Property(s => s.FrequencyType).HasConversion<string>();
                schedule.Property(s => s.Unit).HasMaxLength(50);
                
                // Configure complex properties
                schedule.Property(s => s.DaysOfWeek)
                    .HasConversion(
                        v => v != null ? string.Join(',', v.Select(d => (int)d)) : null,
                        v => !string.IsNullOrEmpty(v) ? v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => (DayOfWeek)int.Parse(s)).ToList() : null);
                
                schedule.Property(s => s.TimesOfDay)
                    .HasConversion(
                        v => string.Join(',', v.Select(t => t.ToString("HH:mm"))),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(TimeOnly.Parse).ToList());
            });
        });

        // Configure MedicationLog entity
        modelBuilder.Entity<MedicationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            // Configure relationships
            // UserId is a foreign key reference to User in ConfigurationDb
            entity.Property(e => e.UserId).IsRequired();
                
            entity.HasOne<Models.Medication>()
                .WithMany()
                .HasForeignKey(e => e.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
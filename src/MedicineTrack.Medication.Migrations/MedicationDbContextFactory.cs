using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MedicineTrack.Medication.Data;

namespace MedicineTrack.Medication.Migrations;

/// <summary>
/// Design-time factory for creating MedicationDbContext instances during migrations
/// </summary>
public class MedicationDbContextFactory : IDesignTimeDbContextFactory<MedicationDbContext>
{
    public MedicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MedicationDbContext>();
        
        // Use a default connection string for migrations
        // This will be overridden at runtime by the actual connection string
        var connectionString = "Host=localhost;Database=medicinetrack_medication;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly("MedicineTrack.Medication.Migrations"));
        
        return new MedicationDbContext(optionsBuilder.Options);
    }
}

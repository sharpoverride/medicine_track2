using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MedicineTrack.Configuration.Data;

namespace MedicineTrack.Configuration.Migrations;

/// <summary>
/// Design-time factory for creating ConfigurationDbContext instances during migrations
/// </summary>
public class ConfigurationDbContextFactory : IDesignTimeDbContextFactory<ConfigurationDbContext>
{
    public ConfigurationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ConfigurationDbContext>();
        
        // Use a default connection string for migrations
        // This will be overridden at runtime by the actual connection string
        var connectionString = "Host=localhost;Database=medicinetrack_configuration;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly("MedicineTrack.Configuration.Migrations"));
        
        return new ConfigurationDbContext(optionsBuilder.Options);
    }
}

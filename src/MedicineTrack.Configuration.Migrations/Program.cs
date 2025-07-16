using Microsoft.EntityFrameworkCore;
using MedicineTrack.Configuration.Data;

namespace MedicineTrack.Configuration.Migrations;

/// <summary>
/// Console application for managing configuration database migrations
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("MedicineTrack Configuration Database Migration Tool");
        
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run [migrate|seed]");
            return;
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ConfigurationDb") 
            ?? "Host=localhost;Database=medicinetrack_configuration;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ConfigurationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        using var context = new ConfigurationDbContext(optionsBuilder.Options);

        switch (args[0].ToLower())
        {
            case "migrate":
                Console.WriteLine("Applying migrations...");
                await context.Database.MigrateAsync();
                Console.WriteLine("Migrations applied successfully.");
                break;
                
            case "seed":
                Console.WriteLine("Seeding database...");
                await SeedDatabase(context);
                Console.WriteLine("Database seeded successfully.");
                break;
                
            default:
                Console.WriteLine("Unknown command. Use 'migrate' or 'seed'.");
                break;
        }
    }

    private static async Task SeedDatabase(ConfigurationDbContext context)
    {
        // Add any seed data logic here if needed
        await context.SaveChangesAsync();
    }
}
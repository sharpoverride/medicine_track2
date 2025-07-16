using Microsoft.EntityFrameworkCore;
using MedicineTrack.Medication.Data;

namespace MedicineTrack.Medication.Migrations;

/// <summary>
/// Console application for managing medication database migrations
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("MedicineTrack Medication Database Migration Tool");
        
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run [migrate|seed]");
            return;
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__medicationdb") 
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__MedicationDb")
            ?? "Host=localhost;Database=medicinetrack_medication;Username=postgres;Password=postgres";
        
        Console.WriteLine($"Using connection string: {connectionString}");

        var optionsBuilder = new DbContextOptionsBuilder<MedicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly("MedicineTrack.Medication.Migrations"));

        using var context = new MedicationDbContext(optionsBuilder.Options);

        switch (args[0].ToLower())
        {
            case "migrate":
                Console.WriteLine("Applying migrations...");
                await RetryOperation(async () => await context.Database.MigrateAsync());
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

    private static async Task SeedDatabase(MedicationDbContext context)
    {
        // Add any seed data logic here if needed
        await context.SaveChangesAsync();
    }

    private static async Task RetryOperation(Func<Task> operation, int maxRetries = 5, int delayMs = 2000)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                Console.WriteLine($"Attempt {i + 1} failed: {ex.Message}");
                Console.WriteLine($"Retrying in {delayMs}ms...");
                await Task.Delay(delayMs);
            }
        }
    }
}

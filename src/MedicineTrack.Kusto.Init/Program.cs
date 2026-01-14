using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;

Console.WriteLine("Kusto Schema Initialization");
Console.WriteLine("===========================");

// Configuration
var kustoConnectionString = Environment.GetEnvironmentVariable("Kusto__ConnectionString")
    ?? "http://localhost:8080";
var databaseName = "medicinetrack";

Console.WriteLine($"Target Kusto: {kustoConnectionString}");
Console.WriteLine($"Database: {databaseName}");

// Create connection string builder for emulator (no authentication)
var kcsb = new KustoConnectionStringBuilder(kustoConnectionString)
    .WithAadUserPromptAuthentication(); // Will fall back to no auth for emulator

// Retry logic for Kusto emulator startup
const int maxRetries = 10;
const int retryDelayMs = 5000;

ICslAdminProvider? adminClient = null;

for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        Console.WriteLine($"Attempt {attempt}/{maxRetries}: Connecting to Kusto emulator...");
        adminClient = KustoClientFactory.CreateCslAdminProvider(kcsb);

        // Test connection
        var testResult = await adminClient.ExecuteControlCommandAsync("", ".show databases");
        Console.WriteLine("✓ Connected to Kusto emulator successfully");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Connection attempt {attempt} failed: {ex.Message}");

        if (attempt < maxRetries)
        {
            Console.WriteLine($"Waiting {retryDelayMs / 1000} seconds before retry...");
            await Task.Delay(retryDelayMs);
        }
        else
        {
            Console.WriteLine("✗ Max retries reached. Kusto emulator may not be running.");
            Console.WriteLine("Please ensure the Kusto emulator container is started.");
            return 1;
        }
    }
}

if (adminClient == null)
{
    Console.WriteLine("✗ Failed to create Kusto admin client");
    return 1;
}

try
{
    // Read schema script
    var scriptPath = Path.Combine(AppContext.BaseDirectory, "init-kusto-schema.kql");

    if (!File.Exists(scriptPath))
    {
        Console.WriteLine($"✗ Schema script not found at: {scriptPath}");
        return 1;
    }

    var schemaScript = await File.ReadAllTextAsync(scriptPath);
    Console.WriteLine($"✓ Loaded schema script from: {scriptPath}");

    // Execute schema creation commands
    Console.WriteLine("Executing schema commands...");

    // Split script into individual commands (separated by blank lines)
    var commands = schemaScript
        .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
        .Where(cmd => !string.IsNullOrWhiteSpace(cmd) && !cmd.Trim().StartsWith("//"))
        .ToList();

    Console.WriteLine($"Found {commands.Count} command blocks to execute");

    foreach (var command in commands)
    {
        try
        {
            var trimmedCommand = command.Trim();
            if (string.IsNullOrWhiteSpace(trimmedCommand)) continue;

            Console.WriteLine($"Executing: {trimmedCommand.Split('\n')[0]}...");
            await adminClient.ExecuteControlCommandAsync("", trimmedCommand);
            Console.WriteLine("✓ Success");
        }
        catch (Exception ex)
        {
            // Some commands may fail if already exists - that's okay
            if (ex.Message.Contains("already exists") || ex.Message.Contains("EntityAlreadyExists"))
            {
                Console.WriteLine($"⚠ Already exists (skipping): {ex.Message}");
            }
            else
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                throw;
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine("===========================");
    Console.WriteLine("✓ Schema initialization completed successfully!");
    Console.WriteLine();
    Console.WriteLine($"Database '{databaseName}' is ready with the following tables:");
    Console.WriteLine("  - traces: Application logs and structured traces");
    Console.WriteLine("  - requests: HTTP requests with duration and result codes");
    Console.WriteLine("  - dependencies: External calls (database, HTTP, etc.)");
    Console.WriteLine("  - exceptions: Application exceptions with stack traces");
    Console.WriteLine();

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Fatal error during schema initialization: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    return 1;
}
finally
{
    adminClient?.Dispose();
}

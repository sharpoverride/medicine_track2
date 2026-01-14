using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using MedicineTrack.RavenDB.Ingestion.Models;
using System.Text;
using System.Text.Json;

namespace MedicineTrack.RavenDB.Ingestion.Services;

/// <summary>
/// Implements telemetry ingestion into Kusto using inline commands
/// </summary>
public class KustoIngestionService : IKustoIngestionService, IDisposable
{
    private readonly ICslAdminProvider _adminClient;
    private readonly ILogger<KustoIngestionService> _logger;
    private const string DatabaseName = "medicinetrack";

    public KustoIngestionService(IConfiguration configuration, ILogger<KustoIngestionService> logger)
    {
        _logger = logger;

        var kustoConnectionString = configuration.GetValue<string>("Kusto__ConnectionString")
            ?? "http://localhost:8080";

        _logger.LogInformation("Initializing Kusto ingestion service with connection: {Connection}", kustoConnectionString);

        var kcsb = new KustoConnectionStringBuilder(kustoConnectionString)
            .WithAadUserPromptAuthentication(); // Falls back to no auth for emulator

        _adminClient = KustoClientFactory.CreateCslAdminProvider(kcsb);
    }

    public async Task IngestTracesAsync(IEnumerable<TraceData> traces, CancellationToken cancellationToken = default)
    {
        var tracesList = traces.ToList();
        if (tracesList.Count == 0)
        {
            _logger.LogDebug("No traces to ingest");
            return;
        }

        _logger.LogInformation("Ingesting {Count} traces to Kusto", tracesList.Count);

        try
        {
            var csvLines = tracesList.Select(t => string.Join(",",
                $"{t.Timestamp:O}",
                EscapeCsv(t.Message),
                t.SeverityLevel,
                EscapeCsv(t.OperationName),
                EscapeCsv(t.OperationId),
                EscapeCsv(t.CloudRoleName),
                EscapeCsv(t.CloudRoleInstance),
                EscapeCsv(SerializeDynamic(t.CustomDimensions)),
                EscapeCsv(t.ItemType)
            ));

            var csv = string.Join("\n", csvLines);
            var command = $".ingest inline into table traces <|\n{csv}";

            await _adminClient.ExecuteControlCommandAsync(DatabaseName, command);
            _logger.LogInformation("Successfully ingested {Count} traces", tracesList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest {Count} traces", tracesList.Count);
            throw;
        }
    }

    public async Task IngestRequestsAsync(IEnumerable<RequestData> requests, CancellationToken cancellationToken = default)
    {
        var requestsList = requests.ToList();
        if (requestsList.Count == 0)
        {
            _logger.LogDebug("No requests to ingest");
            return;
        }

        _logger.LogInformation("Ingesting {Count} requests to Kusto", requestsList.Count);

        try
        {
            var csvLines = requestsList.Select(r => string.Join(",",
                $"{r.Timestamp:O}",
                EscapeCsv(r.Name),
                EscapeCsv(r.Url),
                EscapeCsv(r.Success),
                EscapeCsv(r.ResultCode),
                r.Duration,
                EscapeCsv(r.OperationName),
                EscapeCsv(r.OperationId),
                EscapeCsv(r.CloudRoleName),
                EscapeCsv(r.CloudRoleInstance),
                EscapeCsv(SerializeDynamic(r.CustomDimensions)),
                EscapeCsv(SerializeMeasurements(r.CustomMeasurements)),
                EscapeCsv(r.ItemType)
            ));

            var csv = string.Join("\n", csvLines);
            var command = $".ingest inline into table requests <|\n{csv}";

            await _adminClient.ExecuteControlCommandAsync(DatabaseName, command);
            _logger.LogInformation("Successfully ingested {Count} requests", requestsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest {Count} requests", requestsList.Count);
            throw;
        }
    }

    public async Task IngestDependenciesAsync(IEnumerable<DependencyData> dependencies, CancellationToken cancellationToken = default)
    {
        var dependenciesList = dependencies.ToList();
        if (dependenciesList.Count == 0)
        {
            _logger.LogDebug("No dependencies to ingest");
            return;
        }

        _logger.LogInformation("Ingesting {Count} dependencies to Kusto", dependenciesList.Count);

        try
        {
            var csvLines = dependenciesList.Select(d => string.Join(",",
                $"{d.Timestamp:O}",
                EscapeCsv(d.Name),
                EscapeCsv(d.Type),
                EscapeCsv(d.Target),
                EscapeCsv(d.Data),
                EscapeCsv(d.Success),
                EscapeCsv(d.ResultCode),
                d.Duration,
                EscapeCsv(d.OperationName),
                EscapeCsv(d.OperationId),
                EscapeCsv(d.CloudRoleName),
                EscapeCsv(d.CloudRoleInstance),
                EscapeCsv(SerializeDynamic(d.CustomDimensions)),
                EscapeCsv(d.ItemType)
            ));

            var csv = string.Join("\n", csvLines);
            var command = $".ingest inline into table dependencies <|\n{csv}";

            await _adminClient.ExecuteControlCommandAsync(DatabaseName, command);
            _logger.LogInformation("Successfully ingested {Count} dependencies", dependenciesList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest {Count} dependencies", dependenciesList.Count);
            throw;
        }
    }

    public async Task IngestExceptionsAsync(IEnumerable<ExceptionData> exceptions, CancellationToken cancellationToken = default)
    {
        var exceptionsList = exceptions.ToList();
        if (exceptionsList.Count == 0)
        {
            _logger.LogDebug("No exceptions to ingest");
            return;
        }

        _logger.LogInformation("Ingesting {Count} exceptions to Kusto", exceptionsList.Count);

        try
        {
            var csvLines = exceptionsList.Select(e => string.Join(",",
                $"{e.Timestamp:O}",
                EscapeCsv(e.ProblemId),
                EscapeCsv(e.Type),
                EscapeCsv(e.Message),
                EscapeCsv(e.OuterType),
                EscapeCsv(e.OuterMessage),
                EscapeCsv(e.InnermostType),
                EscapeCsv(e.InnermostMessage),
                e.SeverityLevel,
                EscapeCsv(e.Stack),
                EscapeCsv(e.OperationName),
                EscapeCsv(e.OperationId),
                EscapeCsv(e.CloudRoleName),
                EscapeCsv(e.CloudRoleInstance),
                EscapeCsv(SerializeDynamic(e.CustomDimensions)),
                EscapeCsv(e.ItemType)
            ));

            var csv = string.Join("\n", csvLines);
            var command = $".ingest inline into table exceptions <|\n{csv}";

            await _adminClient.ExecuteControlCommandAsync(DatabaseName, command);
            _logger.LogInformation("Successfully ingested {Count} exceptions", exceptionsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest {Count} exceptions", exceptionsList.Count);
            throw;
        }
    }

    // Helper methods for CSV escaping and serialization

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        // Escape quotes and wrap in quotes
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string SerializeDynamic(Dictionary<string, object?>? dict)
    {
        if (dict == null || dict.Count == 0)
            return "{}";

        try
        {
            return JsonSerializer.Serialize(dict);
        }
        catch
        {
            return "{}";
        }
    }

    private static string SerializeMeasurements(Dictionary<string, double>? measurements)
    {
        if (measurements == null || measurements.Count == 0)
            return "{}";

        try
        {
            return JsonSerializer.Serialize(measurements);
        }
        catch
        {
            return "{}";
        }
    }

    public void Dispose()
    {
        _adminClient?.Dispose();
    }
}

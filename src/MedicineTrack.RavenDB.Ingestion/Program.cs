using System.IO.Compression;
using System.Text.Json;
using MedicineTrack.RavenDB.Ingestion.Indexes;
using MedicineTrack.RavenDB.Ingestion.Models;
using MedicineTrack.RavenDB.Ingestion.Models.Otlp;
using MedicineTrack.RavenDB.Ingestion.Services;
using Microsoft.AspNetCore.Http.Json;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON options for OTLP compatibility (camelCase)
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Add OpenTelemetry
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// Configure RavenDB DocumentStore
var ravenDbUrl = builder.Configuration.GetValue<string>("RavenDB:Url")
    ?? Environment.GetEnvironmentVariable("RavenDB__Url")
    ?? "https://ravendb.ravendb.orb.local";

var ravenDbDatabase = builder.Configuration.GetValue<string>("RavenDB:Database")
    ?? Environment.GetEnvironmentVariable("RavenDB__Database")
    ?? "telemetry";

builder.Services.AddSingleton<IDocumentStore>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Configuring RavenDB DocumentStore");
    logger.LogInformation("  URL: {RavenDbUrl}", ravenDbUrl);
    logger.LogInformation("  Database: {RavenDbDatabase}", ravenDbDatabase);

    var store = new DocumentStore
    {
        Urls = [ravenDbUrl],
        Database = ravenDbDatabase
    };

    // Disable topology updates for local development
    store.Conventions.DisableTopologyUpdates = false;

    try
    {
        store.Initialize();
        logger.LogInformation("RavenDB DocumentStore initialized successfully");

        // Deploy indexes
        logger.LogInformation("Deploying RavenDB indexes");
        IndexCreation.CreateIndexes(typeof(Traces_ByServiceAndTime).Assembly, store);
        logger.LogInformation("RavenDB indexes deployed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize RavenDB DocumentStore or deploy indexes");
        throw;
    }

    return store;
});

// Add RavenDB ingestion service
builder.Services.AddSingleton<IRavenDBIngestionService, RavenDBIngestionService>();

var app = builder.Build();

var logger = app.Logger;

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ravendb-ingestion" }))
    .WithName("HealthCheck")
    .WithTags("Health");

// OTLP standard endpoints (for OpenTelemetry Collector)
app.MapPost("/v1/traces", async (
    HttpContext context,
    IRavenDBIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Read request body into memory to detect gzip compression
        using var memoryStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        Stream requestBody = memoryStream;

        // Check for gzip magic bytes (0x1F 0x8B) at start of stream
        byte[] buffer = new byte[2];
        int bytesRead = await memoryStream.ReadAsync(buffer, cancellationToken);
        memoryStream.Position = 0;

        if (bytesRead >= 2 && buffer[0] == 0x1F && buffer[1] == 0x8B)
        {
            logger.LogDebug("Detected gzip-compressed payload, decompressing");
            requestBody = new GZipStream(memoryStream, CompressionMode.Decompress);
        }

        // Manually deserialize to get better error messages
        OtlpTraceRequest? otlpRequest;
        try
        {
            otlpRequest = await JsonSerializer.DeserializeAsync<OtlpTraceRequest>(requestBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                },
                cancellationToken);

            if (otlpRequest == null)
            {
                logger.LogWarning("Received null OTLP trace request");
                return Results.BadRequest(new { error = "Request body is null" });
            }
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Failed to deserialize OTLP trace request");
            return Results.BadRequest(new { error = "Invalid JSON format", details = jsonEx.Message });
        }

        logger.LogInformation("Received OTLP traces with {Count} resource spans", otlpRequest.ResourceSpans.Count);

        var (traces, requests) = OtlpMapper.MapTraces(otlpRequest);

        if (traces.Count > 0)
        {
            await ingestionService.IngestTracesAsync(traces.ToArray(), cancellationToken);
            logger.LogInformation("Ingested {Count} traces from OTLP", traces.Count);
        }

        if (requests.Count > 0)
        {
            await ingestionService.IngestRequestsAsync(requests.ToArray(), cancellationToken);
            logger.LogInformation("Ingested {Count} requests from OTLP", requests.Count);
        }

        return Results.Ok(new { ingested = traces.Count + requests.Count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ingest OTLP traces");
        return Results.Problem(
            title: "OTLP Ingestion Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("IngestOtlpTraces")
.WithTags("OTLP")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/v1/logs", async (
    HttpContext context,
    IRavenDBIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Read request body into memory to detect gzip compression
        using var memoryStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        Stream requestBody = memoryStream;

        // Check for gzip magic bytes (0x1F 0x8B) at start of stream
        byte[] buffer = new byte[2];
        int bytesRead = await memoryStream.ReadAsync(buffer, cancellationToken);
        memoryStream.Position = 0;

        if (bytesRead >= 2 && buffer[0] == 0x1F && buffer[1] == 0x8B)
        {
            logger.LogDebug("Detected gzip-compressed payload, decompressing");
            requestBody = new GZipStream(memoryStream, CompressionMode.Decompress);
        }

        // Manually deserialize to get better error messages
        OtlpLogsRequest? otlpRequest;
        try
        {
            otlpRequest = await JsonSerializer.DeserializeAsync<OtlpLogsRequest>(requestBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                },
                cancellationToken);

            if (otlpRequest == null)
            {
                logger.LogWarning("Received null OTLP logs request");
                return Results.BadRequest(new { error = "Request body is null" });
            }
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Failed to deserialize OTLP logs request");
            return Results.BadRequest(new { error = "Invalid JSON format", details = jsonEx.Message });
        }

        logger.LogInformation("Received OTLP logs with {Count} resource logs", otlpRequest.ResourceLogs.Count);

        var traces = OtlpMapper.MapLogs(otlpRequest);

        if (traces.Count > 0)
        {
            await ingestionService.IngestTracesAsync(traces.ToArray(), cancellationToken);
            logger.LogInformation("Ingested {Count} log traces from OTLP", traces.Count);
        }

        return Results.Ok(new { ingested = traces.Count });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ingest OTLP logs");
        return Results.Problem(
            title: "OTLP Ingestion Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("IngestOtlpLogs")
.WithTags("OTLP")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

// Custom ingestion endpoints (for manual testing)
app.MapPost("/ingest/traces", async (
    TraceDocument[] traces,
    IRavenDBIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("Received {Count} traces for ingestion", traces.Length);
        await ingestionService.IngestTracesAsync(traces, cancellationToken);
        return Results.Ok(new { ingested = traces.Length, collection = "Traces" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ingest traces");
        return Results.Problem(
            title: "Ingestion Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("IngestTraces")
.WithTags("Ingestion")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/ingest/requests", async (
    RequestDocument[] requests,
    IRavenDBIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("Received {Count} requests for ingestion", requests.Length);
        await ingestionService.IngestRequestsAsync(requests, cancellationToken);
        return Results.Ok(new { ingested = requests.Length, collection = "Requests" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ingest requests");
        return Results.Problem(
            title: "Ingestion Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("IngestRequests")
.WithTags("Ingestion")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/ingest/dependencies", async (
    DependencyDocument[] dependencies,
    IRavenDBIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("Received {Count} dependencies for ingestion", dependencies.Length);
        await ingestionService.IngestDependenciesAsync(dependencies, cancellationToken);
        return Results.Ok(new { ingested = dependencies.Length, collection = "Dependencies" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ingest dependencies");
        return Results.Problem(
            title: "Ingestion Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("IngestDependencies")
.WithTags("Ingestion")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/ingest/exceptions", async (
    ExceptionDocument[] exceptions,
    IRavenDBIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("Received {Count} exceptions for ingestion", exceptions.Length);
        await ingestionService.IngestExceptionsAsync(exceptions, cancellationToken);
        return Results.Ok(new { ingested = exceptions.Length, collection = "Exceptions" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ingest exceptions");
        return Results.Problem(
            title: "Ingestion Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("IngestExceptions")
.WithTags("Ingestion")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

logger.LogInformation("RavenDB Ingestion service started. Endpoints available:");
logger.LogInformation("  OTLP (OpenTelemetry Collector):");
logger.LogInformation("    - POST /v1/traces");
logger.LogInformation("    - POST /v1/logs");
logger.LogInformation("  Custom (Manual testing):");
logger.LogInformation("    - POST /ingest/traces");
logger.LogInformation("    - POST /ingest/requests");
logger.LogInformation("    - POST /ingest/dependencies");
logger.LogInformation("    - POST /ingest/exceptions");
logger.LogInformation("  Health:");
logger.LogInformation("    - GET /health");

app.Run();

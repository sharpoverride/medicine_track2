using MedicineTrack.RavenDB.Ingestion.Models;
using MedicineTrack.RavenDB.Ingestion.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

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

// Add RavenDB ingestion service
builder.Services.AddSingleton<IKustoIngestionService, KustoIngestionService>();

var app = builder.Build();

var logger = app.Logger;

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ravendb-ingestion" }))
    .WithName("HealthCheck")
    .WithTags("Health");

// Ingestion endpoints
app.MapPost("/ingest/traces", async (
    TraceData[] traces,
    IKustoIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("Received {Count} traces for ingestion", traces.Length);
        await ingestionService.IngestTracesAsync(traces, cancellationToken);
        return Results.Ok(new { ingested = traces.Length, table = "traces" });
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
    RequestData[] requests,
    IKustoIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("Received {Count} requests for ingestion", requests.Length);
        await ingestionService.IngestRequestsAsync(requests, cancellationToken);
        return Results.Ok(new { ingested = requests.Length, table = "requests" });
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
    DependencyData[] dependencies,
    IKustoIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("Received {Count} dependencies for ingestion", dependencies.Length);
        await ingestionService.IngestDependenciesAsync(dependencies, cancellationToken);
        return Results.Ok(new { ingested = dependencies.Length, table = "dependencies" });
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
    ExceptionData[] exceptions,
    IKustoIngestionService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        logger.LogInformation("Received {Count} exceptions for ingestion", exceptions.Length);
        await ingestionService.IngestExceptionsAsync(exceptions, cancellationToken);
        return Results.Ok(new { ingested = exceptions.Length, table = "exceptions" });
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
logger.LogInformation("  - POST /ingest/traces");
logger.LogInformation("  - POST /ingest/requests");
logger.LogInformation("  - POST /ingest/dependencies");
logger.LogInformation("  - POST /ingest/exceptions");
logger.LogInformation("  - GET /health");

app.Run();

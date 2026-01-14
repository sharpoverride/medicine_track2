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

// Add services
// TODO: Register IKustoIngestionService in KUSTO-3.4

var app = builder.Build();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "kusto-ingestion" }));

// TODO: Add ingestion endpoints in KUSTO-3.5

app.Run();

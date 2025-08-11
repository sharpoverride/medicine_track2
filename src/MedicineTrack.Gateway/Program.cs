using Yarp.ReverseProxy.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry logging to feed Aspire dashboard structured logs
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter();
});
// Enrich logs with Activity context so TraceId/SpanId are present in structured logs
builder.Services.Configure<LoggerFactoryOptions>(o =>
{
    o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.Baggage | ActivityTrackingOptions.Tags;
});

// Add OpenTelemetry tracing so inbound/outbound HTTP are correlated
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// Add services to the container.
builder.Services.AddOpenApi();

// Add YARP services
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors();
}

app.UseHttpsRedirection();

// Lightweight request/response logging for gateway traffic (no bodies)
app.Use(async (context, next) =>
{
    var logger = app.Logger;
    var method = context.Request.Method;
    var path = context.Request.Path.Value;
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var traceId = Activity.Current?.TraceId.ToString();
    var sw = Stopwatch.StartNew();

    logger.LogInformation("Gateway request started {Method} {Path}{Query} traceId={TraceId}", method, path, query, traceId);
    await next();
    sw.Stop();

    var status = context.Response.StatusCode;
    var length = context.Response.ContentLength;
    logger.LogInformation("Gateway response completed {Method} {Path}{Query} -> {Status} ({ElapsedMs} ms) contentLength={ContentLength} traceId={TraceId}",
        method, path, query, status, sw.ElapsedMilliseconds, length, traceId);
});

// Health check endpoint
app.MapGet("/health", (ILogger<Program> logger) =>
{
    logger.LogInformation("Health endpoint hit");
    return "Gateway is healthy";
})
    .WithName("HealthCheck")
    .WithOpenApi();

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();

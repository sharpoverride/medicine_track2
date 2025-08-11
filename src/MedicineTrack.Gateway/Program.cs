using Yarp.ReverseProxy.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry logging to feed Aspire dashboard structured logs
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter();
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

// Health check endpoint
app.MapGet("/health", () => "Gateway is healthy")
    .WithName("HealthCheck")
    .WithOpenApi();

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();

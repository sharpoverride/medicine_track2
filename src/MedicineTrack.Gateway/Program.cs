using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

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

using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Npgsql;

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

// Add OpenTelemetry tracing for inbound/outbound HTTP
builder.Services.AddOpenTelemetry()
.WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Npgsql")
        .AddOtlpExporter());

// Add services to the container.
builder.Services.AddOpenApi();

// Configure JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var baseApiRoute = "";

// --- Organizations Management Endpoints ---
var organizationsGroup = app.MapGroup($"{baseApiRoute}/organizations").WithTags("Organizations");

organizationsGroup.MapPost("/", ([FromBody] CreateOrganizationRequest req) =>
{
    // Placeholder: In a real app, save to DB
    var newOrganization = new Organization(
        Guid.NewGuid(), req.Name, req.Description, req.ContactEmail,
        req.Address, req.PhoneNumber, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow
    );
    return Results.Created($"{baseApiRoute}/organizations/{newOrganization.Id}", newOrganization);
})
.Produces<Organization>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

organizationsGroup.MapGet("/", (string? search, bool? isActive) =>
{
    // Placeholder: Filter and retrieve from DB
    var sampleOrganizations = new List<Organization>
    {
        new Organization(Guid.NewGuid(), "Healthcare Corp", "Leading healthcare provider", "contact@healthcare.com",
                        "123 Medical St, City, State 12345", "+1-555-0123", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    };
    return Results.Ok(sampleOrganizations);
})
.Produces<List<Organization>>();

organizationsGroup.MapGet("/{organizationId:guid}", (Guid organizationId) =>
{
    // Placeholder: Retrieve from DB
    var organization = new Organization(organizationId, "Healthcare Corp", "Leading healthcare provider", "contact@healthcare.com",
                                      "123 Medical St, City, State 12345", "+1-555-0123", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    return Results.Ok(organization);
})
.Produces<Organization>()
.Produces(StatusCodes.Status404NotFound);

organizationsGroup.MapPut("/{organizationId:guid}", (Guid organizationId, [FromBody] UpdateOrganizationRequest req) =>
{
    // Placeholder: Update in DB
    var updatedOrganization = new Organization(
        organizationId, req.Name ?? "Existing Name", req.Description, req.ContactEmail ?? "existing@email.com",
        req.Address, req.PhoneNumber, req.IsActive ?? true, DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow
    );
    return Results.Ok(updatedOrganization);
})
.Produces<Organization>()
.Produces(StatusCodes.Status404NotFound);

organizationsGroup.MapDelete("/{organizationId:guid}", (Guid organizationId) =>
{
    // Placeholder: Soft delete or archive
    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// --- Users Management Endpoints ---
var usersGroup = app.MapGroup($"{baseApiRoute}/organizations/{{organizationId:guid}}/users").WithTags("Users");

usersGroup.MapPost("/", (Guid organizationId, [FromBody] CreateUserRequest req) =>
{
    // Placeholder: Create user and associate with organization
    var newUser = new User(
        Guid.NewGuid(), organizationId, req.Email, req.Name, req.Role,
        req.PhoneNumber, req.Timezone ?? "UTC", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow
    );
    return Results.Created($"{baseApiRoute}/organizations/{organizationId}/users/{newUser.Id}", newUser);
})
.Produces<User>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

usersGroup.MapGet("/", (Guid organizationId, string? search, UserRole? role, bool? isActive) =>
{
    // Placeholder: Get users for organization
    var sampleUsers = new List<User>
    {
        new User(Guid.NewGuid(), organizationId, "user@healthcare.com", "John Doe", UserRole.USER,
                "+1-555-0456", "America/New_York", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
    };
    return Results.Ok(sampleUsers);
})
.Produces<List<User>>();

usersGroup.MapGet("/{userId:guid}", (Guid organizationId, Guid userId) =>
{
    // Placeholder: Get specific user
    var user = new User(userId, organizationId, "user@healthcare.com", "John Doe", UserRole.USER,
                       "+1-555-0456", "America/New_York", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    return Results.Ok(user);
})
.Produces<User>()
.Produces(StatusCodes.Status404NotFound);

usersGroup.MapPut("/{userId:guid}", (Guid organizationId, Guid userId, [FromBody] UpdateUserRequest req) =>
{
    // Placeholder: Update user
    var updatedUser = new User(
        userId, organizationId, req.Email ?? "existing@email.com", req.Name ?? "Existing Name", req.Role ?? UserRole.USER,
        req.PhoneNumber, req.Timezone ?? "UTC", req.IsActive ?? true, DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow
    );
    return Results.Ok(updatedUser);
})
.Produces<User>()
.Produces(StatusCodes.Status404NotFound);

usersGroup.MapDelete("/{userId:guid}", (Guid organizationId, Guid userId) =>
{
    // Placeholder: Soft delete user
    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// Health check
app.MapGet("/health", async (ILogger<Program> logger, IConfiguration config) =>
{
    logger.LogInformation("Health endpoint hit");

    try
    {
        var cs = config.GetConnectionString("configurationdb") ?? config.GetConnectionString("ConfigurationDb");
        if (!string.IsNullOrWhiteSpace(cs))
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            var result = await cmd.ExecuteScalarAsync();
            logger.LogDebug("DB ping result: {Result}", result);
        }
        else
        {
            logger.LogWarning("No connection string found for configurationdb; skipping DB ping");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Health DB ping failed");
    }

    return "Configuration API is healthy";
})
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

// --- Enums ---
public enum UserRole { ADMIN, MANAGER, USER }

// --- Data Models ---
public record Organization(
    Guid Id,
    string Name,
    string? Description,
    string ContactEmail,
    string? Address,
    string? PhoneNumber,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record User(
    Guid Id,
    Guid OrganizationId,
    string Email,
    string Name,
    UserRole Role,
    string? PhoneNumber,
    string Timezone,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

// --- Request DTOs ---
public record CreateOrganizationRequest(
    string Name,
    string? Description,
    string ContactEmail,
    string? Address,
    string? PhoneNumber
);

public record UpdateOrganizationRequest(
    string? Name,
    string? Description,
    string? ContactEmail,
    string? Address,
    string? PhoneNumber,
    bool? IsActive
);

public record CreateUserRequest(
    string Email,
    string Name,
    UserRole Role,
    string? PhoneNumber,
    string? Timezone
);

public record UpdateUserRequest(
    string? Email,
    string? Name,
    UserRole? Role,
    string? PhoneNumber,
    string? Timezone,
    bool? IsActive
);

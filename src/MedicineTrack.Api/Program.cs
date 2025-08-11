using Microsoft.AspNetCore.Mvc;
using MedicineTrack.Api.Configuration;
using MedicineTrack.Api.Models;
using MedicineTrack.Api.DTOs;
using MedicineTrack.Api.Middleware;
using MedicineTrack.Medication.Data.Models;
using MedicineTrack.Configuration.Data.Models;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry logging to feed Aspire dashboard structured logs
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter(); // Honors OTEL_* env vars provided by Aspire
});

// Add services to the container.
builder.Services.AddOpenApi();

// Configure MedicineTrack services and logging
builder.Services.AddMedicineTrackServices();
builder.Services.AddMedicineTrackLogging();

// In a real application, you would add services for database context, authentication, etc.
// builder.Services.AddDbContext<AppDbContext>(options => ...);
// builder.Services.AddAuthentication(...);
// builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseMiddleware<RequestBodyLoggingMiddleware>();
}

app.UseHttpsRedirection();

// In a real application, you would add UseAuthentication and UseAuthorization middleware
// app.UseAuthentication();
// app.UseAuthorization();

var baseApiRoute = "";

// --- Medication Management Endpoints ---
var medicationsGroup = app.MapGroup($"{baseApiRoute}/users/{{userId:guid}}/medications").WithTags("Medications");

medicationsGroup.MapPost("/", (Guid userId, [FromBody] CreateMedicationRequest req) =>
{
    // Placeholder: In a real app, save to DB, generate IDs for medication and schedules
    var newSchedules = req.Schedules.Select(s => new Schedule(
        Guid.NewGuid(), s.FrequencyType, s.Interval, s.DaysOfWeek, s.TimesOfDay, s.Quantity, s.Unit
    )).ToList();

    var newMedication = new Medication
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = req.Name,
        GenericName = req.GenericName,
        BrandName = req.BrandName,
        Strength = req.Strength,
        Form = req.Form,
        Shape = req.Shape,
        Color = req.Color,
        Notes = req.Notes,
        StartDate = req.StartDate,
        EndDate = req.EndDate,
        IsArchived = false,
        Schedules = newSchedules,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
    return Results.Created($"{baseApiRoute}/users/{userId}/medications/{newMedication.Id}", newMedication);
})
.Produces<Medication>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

medicationsGroup.MapGet("/", (Guid userId, string? status, string? search) =>
{
    // Placeholder: Filter and retrieve from DB
    var sampleSchedule = new Schedule(Guid.NewGuid(), FrequencyType.EVERY_X_DAYS, 1, null, new List<TimeOnly> { new TimeOnly(8,0) }, 1, "tablet");
    var sampleMedications = new List<Medication>
    {
        new Medication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Lisinopril",
            GenericName = "Lisinopril",
            BrandName = "Zestril",
            Strength = "10 mg",
            Form = "Tablet",
            Shape = "Round",
            Color = "White",
            Notes = "Take with water",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = null,
            IsArchived = false,
            Schedules = new List<Schedule> { sampleSchedule },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }
    };
    return Results.Ok(sampleMedications);
})
.Produces<List<Medication>>();

medicationsGroup.MapGet("/{medicationId:guid}", (Guid userId, Guid medicationId) =>
{
    // Placeholder: Retrieve from DB
    var sampleSchedule = new Schedule(Guid.NewGuid(), FrequencyType.EVERY_X_DAYS, 1, null, new List<TimeOnly> { new TimeOnly(8,0) }, 1, "tablet");
    var medication = new Medication
    {
        Id = medicationId,
        UserId = userId,
        Name = "Lisinopril",
        GenericName = "Lisinopril",
        BrandName = "Zestril",
        Strength = "10 mg",
        Form = "Tablet",
        Shape = "Round",
        Color = "White",
        Notes = "Take with water",
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        EndDate = null,
        IsArchived = false,
        Schedules = new List<Schedule> { sampleSchedule },
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
    return Results.Ok(medication);
    // return Results.NotFound(); // If not found
})
.Produces<Medication>()
.Produces(StatusCodes.Status404NotFound);

medicationsGroup.MapPut("/{medicationId:guid}", (Guid userId, Guid medicationId, [FromBody] UpdateMedicationRequest req) =>
{
    // Placeholder: Retrieve from DB, update, and save
    var sampleSchedule = new Schedule(Guid.NewGuid(), FrequencyType.EVERY_X_DAYS, 1, null, new List<TimeOnly> { new TimeOnly(8,0) }, 1, "tablet");
    var updatedMedication = new Medication
    {
        Id = medicationId,
        UserId = userId,
        Name = req.Name ?? "Existing Name",
        GenericName = req.GenericName,
        BrandName = req.BrandName,
        Strength = req.Strength ?? "10mg",
        Form = req.Form ?? "Tablet",
        Shape = req.Shape,
        Color = req.Color,
        Notes = req.Notes,
        StartDate = req.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
        EndDate = req.EndDate,
        IsArchived = req.IsArchived ?? false,
        Schedules = req.Schedules?.Select(s => new Schedule(Guid.NewGuid(), s.FrequencyType, s.Interval, s.DaysOfWeek, s.TimesOfDay, s.Quantity, s.Unit)).ToList() ?? new List<Schedule> { sampleSchedule },
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-5), // Simulate existing created date
        UpdatedAt = DateTimeOffset.UtcNow
    };
    return Results.Ok(updatedMedication);
    // return Results.NotFound(); // If not found
})
.Produces<Medication>()
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

medicationsGroup.MapDelete("/{medicationId:guid}", (Guid userId, Guid medicationId) =>
{
    // Placeholder: Archive or delete from DB
    // For soft delete (archiving): find medication, set IsArchived = true, save.
    return Results.NoContent();
    // return Results.NotFound(); // If not found
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);


// --- Medication Logging Endpoints ---
var medicationLogsGroup = app.MapGroup($"{baseApiRoute}/users/{{userId:guid}}").WithTags("Medication Logs");

medicationLogsGroup.MapPost("/medications/{medicationId:guid}/logs", (Guid userId, Guid medicationId, [FromBody] LogMedicationRequest req) =>
{
    // Placeholder: Save log to DB
    var newLog = new MedicationLog(
        Guid.NewGuid(), userId, medicationId, req.ScheduleId, req.TakenAt, req.Status,
        req.QuantityTaken, req.Notes, DateTimeOffset.UtcNow
    );
    return Results.Created($"{baseApiRoute}/users/{userId}/medication-logs/{newLog.Id}", newLog);
})
.Produces<MedicationLog>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

medicationLogsGroup.MapGet("/medication-logs", (Guid userId, Guid? medicationId, DateTime? startDate, DateTime? endDate, LogStatus? status) =>
{
    // Placeholder: Filter and retrieve logs from DB
    var sampleLogs = new List<MedicationLog>
    {
        new MedicationLog(Guid.NewGuid(), userId, medicationId ?? Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-2),
                          LogStatus.TAKEN, 1, "Took with food", DateTimeOffset.UtcNow)
    };
    return Results.Ok(sampleLogs);
})
.Produces<List<MedicationLog>>();

medicationLogsGroup.MapGet("/medications/{medicationId:guid}/logs", (Guid userId, Guid medicationId, DateTime? startDate, DateTime? endDate, LogStatus? status) =>
{
    // Placeholder: Filter and retrieve logs for a specific medication from DB
     var sampleLogs = new List<MedicationLog>
    {
        new MedicationLog(Guid.NewGuid(), userId, medicationId, Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-2),
                          LogStatus.TAKEN, 1, "Took with food", DateTimeOffset.UtcNow)
    };
    return Results.Ok(sampleLogs);
})
.Produces<List<MedicationLog>>();

medicationLogsGroup.MapPut("/medication-logs/{logId:guid}", (Guid userId, Guid logId, [FromBody] UpdateMedicationLogRequest req) =>
{
    // Placeholder: Retrieve log from DB, update, and save
    var updatedLog = new MedicationLog(
        logId, userId, Guid.NewGuid(), // Assuming medicationId is part of the log fetched from DB
        Guid.NewGuid(), // schedule id
        req.TakenAt ?? DateTimeOffset.UtcNow,
        req.Status ?? LogStatus.TAKEN,
        req.QuantityTaken,
        req.Notes,
        // LoggedAt should ideally not change, or have a separate UpdatedAt for the log
        DateTimeOffset.UtcNow 
    );
    return Results.Ok(updatedLog);
    // return Results.NotFound(); // If not found
})
.Produces<MedicationLog>()
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

medicationLogsGroup.MapDelete("/medication-logs/{logId:guid}", (Guid userId, Guid logId) =>
{
    // Placeholder: Delete log from DB
    return Results.NoContent();
    // return Results.NotFound(); // If not found
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);


// --- Medication Database Lookup Endpoint ---
app.MapGet($"{baseApiRoute}/medication-database/search", (string query, string? form, string? strength) =>
{
    // Placeholder: Search external/internal drug database
    var results = new List<MedicationDefinition>
    {
        new MedicationDefinition("0000-0000-00", "Lisinopril (Sample)", "Lisinopril", new List<string>{"Zestril", "Prinivil"},
                                 new List<string>{"Tablet", "Capsule"}, new List<string>{"5 mg", "10 mg", "20 mg"}, "Sample Pharma")
    };
    return Results.Ok(results);
})
.WithTags("Medication Database")
.Produces<List<MedicationDefinition>>();


// --- Medication Interaction Check Endpoint ---
app.MapPost($"{baseApiRoute}/users/{{userId:guid}}/medication-interactions/check", (Guid userId, [FromBody] CheckInteractionRequest req) =>
{
    // Placeholder: Check for interactions using an external service or internal rules engine
    var warnings = new List<MedicationInteractionWarning>();
    if (req.MedicationIds?.Count > 1 || (req.MedicationIds?.Any() == true && req.NewMedication != null))
    {
        warnings.Add(new MedicationInteractionWarning(
            new List<InteractingMedicationInfo> {
                new InteractingMedicationInfo(req.MedicationIds?.FirstOrDefault(), "Medication A"),
                new InteractingMedicationInfo(null, req.NewMedication?.Name ?? "Medication B")
            },
            InteractionSeverity.MODERATE,
            "Sample interaction: Monitor for increased side effects.",
            "Internal Database"
        ));
    }
    return Results.Ok(warnings);
})
.WithTags("Medication Interactions")
.Produces<List<MedicationInteractionWarning>>()
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("health", () => "MedicineTrack API is OK");

app.Run();

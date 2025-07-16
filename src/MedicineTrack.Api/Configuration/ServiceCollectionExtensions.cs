using MedicineTrack.Api.Services;
using MedicineTrack.Api.Repositories;
using System.Text.Json.Serialization;

namespace MedicineTrack.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMedicineTrackServices(this IServiceCollection services)
    {
        // Configure JSON serialization
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Register services (placeholder implementations will be added in later tasks)
        // services.AddScoped<IMedicationService, MedicationService>();
        // services.AddScoped<IMedicationLogService, MedicationLogService>();
        // services.AddScoped<IMedicationDatabaseService, MedicationDatabaseService>();
        // services.AddScoped<IDrugInteractionService, DrugInteractionService>();

        // Register repositories (placeholder implementations will be added in later tasks)
        // services.AddScoped<IMedicationRepository, MedicationRepository>();

        return services;
    }

    public static IServiceCollection AddMedicineTrackLogging(this IServiceCollection services)
    {
        // Configure structured logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        return services;
    }
}
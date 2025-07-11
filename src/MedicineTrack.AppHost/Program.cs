var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure services
var valkeyCache = builder.AddRedis("valkeycache")
    .WithImage("valkey/valkey")
    .WithImageTag("latest");

var db = builder.AddPostgres("postgresdb")
    .AddDatabase("apidb");

// Application services
var apiService = builder.AddProject<Projects.MedicineTrack_Api>("medicine-track-api")
    .WithReference(valkeyCache)
    .WithReference(db)
    .WithHttpEndpoint(port: 5001, name: "api-http");

var configService = builder.AddProject<Projects.MedicineTrack_Configuration>("medicine-track-config")
    .WithReference(valkeyCache)
    .WithReference(db)
    .WithHttpEndpoint(port: 5002, name: "config-http");

// API Gateway - references the backend services
var gatewayService = builder.AddProject<Projects.MedicineTrack_Gateway>("medicine-track-gateway")
    .WithReference(apiService)
    .WithReference(configService)
    .WithHttpEndpoint(port: 5000, name: "gateway-http");

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure services
var valkeyCache = builder.AddRedis("valkeycache")
    .WithImage("valkey/valkey")
    .WithImageTag("latest");

var postgres = builder.AddPostgres("postgresdb");
var medicationDb = postgres.AddDatabase("medicationdb");
var configurationDb = postgres.AddDatabase("configurationdb");

// Migration projects - run these first to set up databases
var medicationMigrations = builder.AddProject<Projects.MedicineTrack_Medication_Migrations>("medication-migrations")
    .WithReference(medicationDb)
    .WithArgs("migrate");

var configurationMigrations = builder
    .AddProject<Projects.MedicineTrack_Configuration_Migrations>("configuration-migrations")
    .WithReference(configurationDb)
    .WithArgs("migrate");

// Application services - wait for migrations to complete
var apiService = builder.AddProject<Projects.MedicineTrack_Api>("medicine-track-api")
    .WithReference(valkeyCache)
    .WithReference(medicationDb)
    .WaitFor(medicationMigrations)
    .WithHttpEndpoint(port: 5001, name: "api-http");

var configService = builder.AddProject<Projects.MedicineTrack_Configuration>("medicine-track-config")
    .WithReference(valkeyCache)
    .WithReference(configurationDb)
    .WaitFor(configurationMigrations)
    .WithHttpEndpoint(port: 5002, name: "config-http");

// API Gateway - references the backend services
var gatewayService = builder.AddProject<Projects.MedicineTrack_Gateway>("medicine-track-gateway")
    .WithReference(apiService)
    .WithReference(configService)
    .WaitFor(apiService)
    .WaitFor(configService)
    .WithHttpEndpoint(port: 5000, name: "gateway-http");

var end2endTestsRunner = builder
    .AddProject<Projects.MedicineTrack_End2EndTests_Runner>("medicine-track-e2e-tests-runner")
    .WithReference(apiService)
    .WithReference(configService)
    .WithReference(gatewayService)
    .WaitFor(gatewayService);
   // .WithArgs("--interval", "10");

var build = builder.Build();
build.Run();
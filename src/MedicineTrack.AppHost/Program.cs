var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure services
var valkeyCache = builder.AddRedis("valkeycache")
    .WithImage("valkey/valkey")
    .WithImageTag("latest");

var postgres = builder.AddPostgres("postgresdb");
var medicationDb = postgres.AddDatabase("medicationdb");
var configurationDb = postgres.AddDatabase("configurationdb");

// OpenTelemetry Collector for telemetry aggregation
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithImageTag("latest")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithBindMount("../../otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml")
    .WithBindMount("../../otel-data", "/var/otel");

// RavenDB Ingestion service
// Configure RavenDB connection (external RavenDB instance)
// Note: Using HTTP localhost since service runs on host (HTTPS via Tailscale has TLS issues)
var ravenDbUrl = builder.Configuration["RavenDB:Url"]
    ?? "http://localhost:8081";
var ravenDbDatabase = builder.Configuration["RavenDB:Database"]
    ?? "telemetry";

var ravenDbIngestion = builder.AddProject<Projects.MedicineTrack_RavenDB_Ingestion>("ravendb-ingestion")
    .WithEnvironment("RavenDB__Url", ravenDbUrl)
    .WithEnvironment("RavenDB__Database", ravenDbDatabase)
    .WithHttpEndpoint(port: 5003, name: "ingestion-http");

// Migration projects - run these first to set up databases
var medicationMigrations = builder.AddProject<Projects.MedicineTrack_Medication_Migrations>("medication-migrations")
    .WithReference(medicationDb)
    .WithArgs("migrate");

var configurationMigrations = builder
    .AddProject<Projects.MedicineTrack_Configuration_Migrations>("configuration-migrations")
    .WithReference(configurationDb)
    .WithArgs("migrate");

// Application services - wait for migrations to complete
// Configure to send telemetry to custom OTEL Collector
var apiService = builder.AddProject<Projects.MedicineTrack_Api>("medicine-track-api")
    .WithReference(valkeyCache)
    .WithReference(medicationDb)
    .WaitFor(medicationMigrations)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318")
    .WithHttpEndpoint(port: 5001, name: "api-http");

var configService = builder.AddProject<Projects.MedicineTrack_Configuration>("medicine-track-config")
    .WithReference(valkeyCache)
    .WithReference(configurationDb)
    .WaitFor(configurationMigrations)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318")
    .WithHttpEndpoint(port: 5002, name: "config-http");

// API Gateway - references the backend services
var gatewayService = builder.AddProject<Projects.MedicineTrack_Gateway>("medicine-track-gateway")
    .WithReference(apiService)
    .WithReference(configService)
    .WaitFor(apiService)
    .WaitFor(configService)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318")
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
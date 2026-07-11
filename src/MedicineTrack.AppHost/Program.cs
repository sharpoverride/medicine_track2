var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure services
var valkeyCache = builder.AddRedis("valkeycache")
    .WithImage("valkey/valkey")
    .WithImageTag("latest");

var postgres = builder.AddPostgres("postgresdb");
var medicationDb = postgres.AddDatabase("medicationdb");
var configurationDb = postgres.AddDatabase("configurationdb");

// ClickHouse telemetry database (local container)
var clickhouse = builder.AddContainer("clickhouse", "clickhouse/clickhouse-server")
    .WithImageTag("latest")
    .WithHttpEndpoint(port: 8123, targetPort: 8123, name: "clickhouse-http")
    .WithEndpoint("clickhouse-native", endpoint =>
    {
        endpoint.UriScheme = "tcp";
        endpoint.Port = 9000;
        endpoint.TargetPort = 9000;
    })
    .WithHttpHealthCheck("/ping", endpointName: "clickhouse-http")
    .WithEnvironment("CLICKHOUSE_DB", "telemetry")
    .WithEnvironment("CLICKHOUSE_USER", "default")
    .WithEnvironment("CLICKHOUSE_PASSWORD", "")
    .WithEnvironment("CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT", "1")
    .WithBindMount("../../clickhouse-init", "/docker-entrypoint-initdb.d")
    .WithBindMount("../../clickhouse-config", "/etc/clickhouse-server/users.d");

// OpenTelemetry Collector for telemetry aggregation
// Use the ClickHouse HTTP endpoint for the exporter; it becomes reachable sooner
// than the native TCP port and avoids the race that crashes the collector on startup.
var clickhouseHttpEndpoint = clickhouse.GetEndpoint("clickhouse-http");
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithImageTag("latest")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithReference(clickhouseHttpEndpoint)
    .WithEnvironment("CLICKHOUSE_ENDPOINT", clickhouseHttpEndpoint)
    .WithBindMount("../../otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml")
    .WithBindMount("../../otel-data", "/var/otel")
    .WaitFor(clickhouse);

// Migration projects - run these first to set up databases
var medicationMigrations = builder.AddProject<Projects.MedicineTrack_Medication_Migrations>("medication-migrations")
    .WithReference(medicationDb)
    .WithArgs("migrate");

var configurationMigrations = builder
    .AddProject<Projects.MedicineTrack_Configuration_Migrations>("configuration-migrations")
    .WithReference(configurationDb)
    .WithArgs("migrate");

// Application services - wait for migrations to complete
// Configure to send telemetry to custom OTEL Collector (gRPC on port 4317)
var apiService = builder.AddProject<Projects.MedicineTrack_Api>("medicine-track-api")
    .WithReference(valkeyCache)
    .WithReference(medicationDb)
    .WaitFor(medicationMigrations)
    .WithReference(otelCollector.GetEndpoint("otlp-grpc"))
    .WithOtlpExporter()
    .WithHttpEndpoint(port: 5001, name: "api-http");

var configService = builder.AddProject<Projects.MedicineTrack_Configuration>("medicine-track-config")
    .WithReference(valkeyCache)
    .WithReference(configurationDb)
    .WaitFor(configurationMigrations)
    .WithReference(otelCollector.GetEndpoint("otlp-grpc"))
    .WithOtlpExporter()
    .WithHttpEndpoint(port: 5002, name: "config-http");

// API Gateway - references the backend services
var gatewayService = builder.AddProject<Projects.MedicineTrack_Gateway>("medicine-track-gateway")
    .WithReference(apiService)
    .WithReference(configService)
    .WaitFor(apiService)
    .WaitFor(configService)
    .WithReference(otelCollector.GetEndpoint("otlp-grpc"))
    .WithOtlpExporter()
    .WithHttpEndpoint(port: 5000, name: "gateway-http");

var end2endTestsRunner = builder
    .AddProject<Projects.MedicineTrack_End2EndTests_Runner>("medicine-track-e2e-tests-runner")
    .WithReference(apiService)
    .WithReference(configService)
    .WithReference(gatewayService)
    .WithReference(otelCollector.GetEndpoint("otlp-grpc"))
    .WithOtlpExporter()
    .WaitFor(gatewayService);
   // .WithArgs("--interval", "10");

var build = builder.Build();
build.Run();
var builder = DistributedApplication.CreateBuilder(args);
var valkeyCache =builder.AddRedis("valkeycache")       // Logical name, will be used for the connection string key
    .WithImage("valkey/valkey")    // Specify the official Valkey Docker image
    .WithImageTag("latest"); 

var db = builder.AddPostgres("postgresdb")   // Aspire can run a PostgreSQL container
    .AddDatabase("apidb");
var apiService = builder.AddProject<Projects.MedicineTrack_Api>("medicine-track-api")
    .WithReference(valkeyCache) // Injects "ConnectionStrings:rediscache"
    .WithReference(db);   
builder.Build().Run();

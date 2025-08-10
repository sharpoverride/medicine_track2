# End-to-End Tests Architecture

## Overview

The MedicineTrack End-to-End testing system is designed to support two distinct execution modes, providing maximum flexibility for different use cases.

## Architecture Components

```
┌─────────────────────────────────────────────────────────────┐
│                    .NET Aspire Host                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────── │
│  │  medicine-track │  │ medicine-track  │  │ End2EndTests   │
│  │     -api        │  │    -config      │  │    .Runner     │
│  │                 │  │                 │  │                │
│  │  Port: 5001     │  │  Port: 5002     │  │ Loads test     │
│  │  (api-http)     │  │  (config-http)  │  │ assembly &     │
│  └─────────────────┘  └─────────────────┘  │ executes tests │
│                                             │ via DI         │
└─────────────────────────────────────────────┴────────────────┘
                              │
                              │ Service Discovery
                              ▼
                    ┌─────────────────┐
                    │ End2EndTests    │
                    │ Assembly        │    ◄── Also can run standalone
                    │                 │        with manual URLs
                    │ - SystemUser    │
                    │   Fixture       │
                    │ - Configuration │
                    │   ApiTests      │
                    │ - Startup.cs    │
                    └─────────────────┘
```

## Execution Modes

### Mode 1: Via End2EndTests.Runner (In-Aspire) ✨

**When to use:** Continuous monitoring, scheduled testing within Aspire dashboard

**How it works:**
1. End2EndTests.Runner runs as an Aspire service
2. Runner creates its own DI container with Aspire service discovery
3. Runner loads `MedicineTrack.End2EndTests.dll` at runtime
4. Runner instantiates test classes using `ActivatorUtilities.CreateInstance()`
5. **Key Point:** Uses Runner's DI container, NOT the test assembly's Startup.cs
6. HttpClients are configured via service discovery: `medicine-track-config` → resolved URL

**Configuration:**
- Uses Aspire's service discovery automatically
- No manual URL configuration needed
- Services resolved via service names

**Launch:**
```bash
cd src/MedicineTrack.End2EndTests.Runner
dotnet run
# or via Aspire dashboard
```

### Mode 2: Direct Execution (Standalone) 🛠️

**When to use:** Development, debugging, CI/CD pipelines

**How it works:**
1. Run `dotnet test` directly on End2EndTests project
2. XUnit discovers Startup.cs and creates DI container
3. Startup.cs configures HttpClients with manual URLs
4. Tests execute using test assembly's DI container
5. **Key Point:** Uses test assembly's Startup.cs configuration

**Configuration:**
- Requires manual URL configuration via environment variables or appsettings.json
- Environment variables take precedence over config files

**Launch:**
```bash
export MEDICINE_TRACK_API_URL="http://localhost:5001"
export MEDICINE_TRACK_CONFIG_URL="http://localhost:5002"
cd src/MedicineTrack.End2EndTests
dotnet test
```

## Service URL Resolution

### In-Aspire Mode (Runner)
```
HttpClient Name: "medicine-track-config"
                        ↓
            Aspire Service Discovery
                        ↓
        Resolution: http://localhost:5002
```

### Standalone Mode (Direct)
```
HttpClient Name: "medicine-track-config"
                        ↓
        Environment Variable Check
    MEDICINE_TRACK_CONFIG_URL=http://localhost:5002
                        ↓
        Manual Configuration: http://localhost:5002
```

## Configuration Files & Their Roles

| File | Used By | Purpose |
|------|---------|---------|
| `End2EndTests/Startup.cs` | Direct execution only | Configures DI for standalone tests |
| `End2EndTests.Runner/Program.cs` | Runner execution only | Configures DI for in-Aspire tests |
| `End2EndTests/appsettings.json` | Direct execution only | Fallback URLs for standalone |
| `End2EndTests.Runner/Properties/launchSettings.json` | Runner execution only | Environment variables for different runner profiles |

## HttpClient Configuration Comparison

### In-Aspire (Runner)
```csharp
services.AddHttpClient("medicine-track-config")
    .AddServiceDiscovery()           // 🎯 Aspire resolves URLs
    .AddStandardResilienceHandler();
```

### Standalone (Direct)
```csharp
services.AddHttpClient("medicine-track-config", client => {
    var baseAddress = Environment.GetEnvironmentVariable("MEDICINE_TRACK_CONFIG_URL") 
        ?? Configuration["Services:ConfigApi"];
    client.BaseAddress = new Uri(baseAddress);  // 🎯 Manual URL setting
});
```

## Troubleshooting

### Runner Shows "BaseAddress is null"
- **Problem:** Runner's DI container not configured properly
- **Solution:** Check End2EndTests.Runner/Program.cs HttpClient configuration

### Direct execution fails with connection refused
- **Problem:** Environment variables not set or services not running
- **Solution:** 
  1. Start Aspire: `cd src/MedicineTrack.AppHost && dotnet run`
  2. Set environment variables: Use setup scripts in `/scripts/`

### Tests work in Runner but not standalone
- **Problem:** Different DI containers, different configurations
- **Solution:** Ensure both Program.cs and Startup.cs are configured correctly

## Best Practices

1. **Development/Debugging**: Use standalone mode for faster iteration
2. **Continuous Monitoring**: Use Runner mode in Aspire dashboard
3. **CI/CD**: Use standalone mode with environment variables
4. **Local Testing**: Use provided setup scripts for easy URL configuration

## Future Enhancements

- Consider unifying the DI configuration to reduce duplication
- Add health check endpoints for better monitoring
- Implement test result persistence for historical tracking
- Add support for different environment configurations (dev, staging, prod)

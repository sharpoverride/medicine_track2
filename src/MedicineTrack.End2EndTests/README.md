# MedicineTrack End-to-End Tests

This project contains end-to-end tests for the MedicineTrack application.

## Configuration

This End-to-End test suite supports two execution modes:

### 1. **Via End2EndTests.Runner** (Running in Aspire) ✨
- Runner executes within the Aspire application host
- Uses Aspire's service discovery to automatically resolve service URLs
- HttpClients are configured by the Runner's DI container with service discovery
- **No manual configuration needed** - service names are resolved automatically

### 2. **Direct Execution** (Standalone `dotnet test`) 🛠️
- Tests run directly against a running Aspire solution
- Uses manual URL configuration from environment variables or appsettings.json
- HttpClients are configured by the End2EndTests Startup.cs
- **Manual configuration required** - URLs must be specified

The tests require proper URLs for the Medicine Track API and Configuration API services. There are multiple ways to configure these URLs:

### 1. Using Environment Variables (Recommended)

Set the following environment variables:

```bash
export MEDICINE_TRACK_API_URL="http://localhost:5001"
export MEDICINE_TRACK_CONFIG_URL="http://localhost:5002"
```

Or for PowerShell:

```powershell
$env:MEDICINE_TRACK_API_URL = "http://localhost:5001"
$env:MEDICINE_TRACK_CONFIG_URL = "http://localhost:5002"
```

### 2. Using Setup Scripts

We provide convenient setup scripts:

**For Bash/Zsh:**
```bash
source scripts/setup-e2e-test-urls.sh
```

**For PowerShell:**
```powershell
.\scripts\setup-e2e-test-urls.ps1
```

### 3. Using Environment File

You can copy and modify the provided environment file:

```bash
cp .env.e2e-tests .env
# Edit .env as needed, then source it:
source .env
```

### 4. Using appsettings.json (Fallback)

If no environment variables are set, the tests will use the URLs from `appsettings.json`:

```json
{
  "Services": {
    "ConfigApi": "http://localhost:5002",
    "MedicationApi": "http://localhost:5001"
  }
}
```

## Available Endpoints

Based on the Aspire dashboard, these are the available service endpoints:

### Medicine Track API Service
- **Primary HTTP**: `http://localhost:5001` (api-http endpoint)
- **Alternative HTTP**: `http://localhost:5155` (http endpoint)
- **HTTPS**: `https://localhost:7001` (https endpoint)

### Medicine Track Config Service
- **Primary HTTP**: `http://localhost:5002` (config-http endpoint)
- **Alternative HTTP**: `http://localhost:5111` (http endpoint)
- **HTTPS**: `https://localhost:7263` (https endpoint)

## Running Tests

Once the URLs are configured, run the tests:

```bash
cd src/MedicineTrack.End2EndTests
dotnet test
```

## Running with Aspire

The tests are designed to work with .NET Aspire. Make sure your Aspire host application is running:

```bash
cd src/MedicineTrack.AppHost
dotnet run
```

Then run the tests with the appropriate configuration.

## Aspire Integration

The tests automatically detect if they're running in an Aspire environment by checking for:
- `OTEL_SERVICE_NAME` environment variable
- `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` environment variable

When Aspire is detected:
- Uses Aspire's service discovery to automatically resolve service names to URLs
- Adds resilience handlers for improved reliability
- No manual URL configuration needed

When running standalone:
- Falls back to manual HttpClient configuration
- Uses environment variables or appsettings.json for URLs

## Test Architecture

- **SystemUserFixture**: Manages test data setup and cleanup
- **ConfigurationApiTests**: Tests for the Configuration API endpoints
- Tests use dependency injection with `IHttpClientFactory` for HTTP communication
- Logging is configured to help with debugging test failures

## Troubleshooting

1. **Connection refused errors**: Make sure the Aspire application is running and services are healthy
2. **Wrong endpoint errors**: Check that environment variables are set correctly
3. **SSL certificate errors**: Use HTTP endpoints for local testing instead of HTTPS

## Environment Variable Priority

The configuration system checks sources in this order:
1. Environment variables (`MEDICINE_TRACK_API_URL`, `MEDICINE_TRACK_CONFIG_URL`)
2. appsettings.json (`Services:ConfigApi`, `Services:MedicationApi`)

Environment variables always take precedence over configuration files.

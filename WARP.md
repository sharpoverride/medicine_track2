# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

MedicineTrack is a comprehensive medication management system built with .NET Aspire. It follows a microservices architecture with separate services for medication management and configuration, backed by PostgreSQL databases and using Redis/Valkey for caching.

## Key Technologies

- **.NET Aspire**: Orchestration and service discovery
- **.NET 9.0**: Core framework
- **PostgreSQL**: Primary database with Entity Framework Core
- **Redis/Valkey**: Caching layer
- **YARP**: API Gateway for reverse proxy
- **xUnit**: Testing framework

## Common Development Commands

### Build and Run

```bash
# Build the entire solution
cd src
dotnet build

# Run with .NET Aspire (recommended)
dotnet run --project MedicineTrack.AppHost

# Access services
# Aspire Dashboard: http://localhost:15888
# API Gateway: http://localhost:5000
# Medicine API: http://localhost:5001
# Configuration API: http://localhost:5002
```

### Testing

```bash
# Run unit tests
dotnet test MedicineTrack.Tests

# Run end-to-end tests (standalone mode)
export MEDICINE_TRACK_API_URL="http://localhost:5001"
export MEDICINE_TRACK_CONFIG_URL="http://localhost:5002"
dotnet test MedicineTrack.End2EndTests

# Run end-to-end tests (via runner in Aspire)
dotnet run --project MedicineTrack.End2EndTests.Runner

# Test both E2E execution modes
../../scripts/test-e2e-execution-modes.sh
```

### Database Migrations

```bash
# Apply medication database migrations
dotnet run --project MedicineTrack.Medication.Migrations -- migrate

# Apply configuration database migrations
dotnet run --project MedicineTrack.Configuration.Migrations -- migrate

# Create new migration (example for medication DB)
dotnet ef migrations add MigrationName --project MedicineTrack.Medication.Migrations --startup-project MedicineTrack.Medication.Migrations --context MedicationDbContext
```

### Deployment

```bash
# Deploy to local Kubernetes cluster
../deploy-aspire.sh

# Manual deployment steps
# 1. Generate Aspire manifest
dotnet run --project MedicineTrack.AppHost --publisher manifest --output-path ../infra/aspire-manifest.json

# 2. Install aspirate if needed
dotnet tool install -g aspirate

# 3. Generate Kubernetes manifests
cd ../infra
aspirate generate \
  --input-path aspire-manifest.json \
  --output-path k8s-manifests \
  --namespace medicine-track \
  --container-registry docker.io \
  --container-image-tag latest

# 4. Apply to Kubernetes
kubectl apply -f k8s-manifests -n medicine-track
```

### Working with Aspire Workload

```bash
# Install Aspire workload if not present
dotnet workload install aspire

# Update Aspire workload
dotnet workload update aspire

# List installed workloads
dotnet workload list
```

## Architecture and Code Structure

### Service Architecture

The application consists of the following key services:

1. **MedicineTrack.AppHost** - .NET Aspire orchestrator that manages all services
2. **MedicineTrack.Gateway** - YARP-based API gateway routing requests to backend services
3. **MedicineTrack.Api** - Main medication management service
4. **MedicineTrack.Configuration** - User and organization management service
5. **Migration Services** - Separate projects for database migrations to ensure schema is ready before services start

### Service Dependencies and Startup Order

The AppHost orchestrates services with proper dependency management:
1. PostgreSQL and Redis/Valkey start first
2. Migration projects run to set up database schemas
3. API services start after migrations complete
4. Gateway starts last, after all backend services are ready

### Database Strategy

The application uses a separated database approach:
- **medicationdb**: Stores medication data, schedules, and logs
- **configurationdb**: Stores organization and user data

Each database has:
- A dedicated Data project containing Entity Framework models and DbContext
- A dedicated Migrations project for schema management
- Migration projects run as part of the Aspire orchestration before services start

### End-to-End Testing Architecture

The E2E tests support two execution modes:

1. **Standalone Mode**: Direct test execution using `dotnet test`
   - Uses manual URL configuration from environment variables
   - Configured via `Startup.cs` in the test project
   
2. **Runner Mode**: Tests run within Aspire as a service
   - Uses Aspire's service discovery for automatic URL resolution
   - Configured via `Program.cs` in the Runner project
   - Enables continuous monitoring within Aspire dashboard

### API Gateway Routing

The Gateway service uses YARP to route requests:
- `/medicines/*` → Medicine API (port 5001)
- `/configs/*` → Configuration API (port 5002)

This provides a single entry point for clients while maintaining service separation.

## Development Tips

### HTTP Testing Files

Use the `.http` files with VS Code REST Client extension for quick API testing:
- `MedicineTrack.Api/Medicines.http` - Medication endpoints
- `MedicineTrack.Api/Health.http` - Health checks
- `MedicineTrack.Configuration/MedicineTrack.Configuration.http` - Configuration endpoints
- `MedicineTrack.Gateway/MedicineTrack.Gateway.http` - Gateway endpoints

### Environment Configuration

For local development with E2E tests:
```bash
# Use the provided setup scripts
source scripts/setup-e2e-test-urls.sh  # Bash/Zsh
# or
./scripts/setup-e2e-test-urls.ps1       # PowerShell
```

### Service Discovery

When running in Aspire, services use names for discovery:
- `medicine-track-api` resolves to the API service
- `medicine-track-config` resolves to the Configuration service
- No hardcoded URLs needed when using Aspire orchestration

### Monitoring and Debugging

- Aspire Dashboard (http://localhost:15888) provides:
  - Service health monitoring
  - Distributed tracing
  - Log aggregation
  - Resource metrics

### Health Endpoints

All services expose health checks:
- Gateway: `GET http://localhost:5000/health`
- Medicine API: `GET http://localhost:5001/health`
- Configuration API: `GET http://localhost:5002/health`

## Key API Endpoints

### Medication Management
- `POST /medicines/users/{userId}/medications` - Create medication
- `GET /medicines/users/{userId}/medications` - List medications
- `PUT /medicines/users/{userId}/medications/{medicationId}` - Update medication
- `DELETE /medicines/users/{userId}/medications/{medicationId}` - Archive medication

### Medication Logging
- `POST /medicines/users/{userId}/medications/{medicationId}/logs` - Log dose
- `GET /medicines/users/{userId}/medication-logs` - Get all logs
- `GET /medicines/users/{userId}/medications/{medicationId}/logs` - Get medication logs

### Drug Interactions
- `POST /medicines/users/{userId}/medication-interactions/check` - Check interactions

### Organization & User Management
- `POST /configs/organizations` - Create organization
- `GET /configs/organizations/{organizationId}` - Get organization
- `POST /configs/organizations/{organizationId}/users` - Create user
- `GET /configs/organizations/{organizationId}/users/{userId}` - Get user

## Troubleshooting

### Service Connection Issues
- Ensure Aspire AppHost is running: `dotnet run --project MedicineTrack.AppHost`
- Check service health via Aspire Dashboard or health endpoints
- Verify PostgreSQL and Redis containers are running

### Migration Failures
- Check connection strings in environment variables
- Verify PostgreSQL is accessible
- Migration projects retry automatically up to 5 times with 2-second delays

### E2E Test Failures
- For standalone mode: Ensure environment variables are set
- For runner mode: Check Aspire service discovery is working
- Verify all services are healthy before running tests

### Kubernetes Deployment Issues
- Ensure kubectl is configured correctly
- Check namespace exists: `kubectl get ns medicine-track`
- Verify all pods are running: `kubectl get pods -n medicine-track`
- Check logs: `kubectl logs -n medicine-track <pod-name>`

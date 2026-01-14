# MedicineTrack API

A comprehensive medication management system built with .NET Aspire that enables users to track medications, schedules, dosing logs, and potential drug interactions.

## 🏗️ Architecture

MedicineTrack follows a microservices architecture built on .NET Aspire, providing a scalable and cloud-native solution for medication management.

### Services Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    API Gateway (Port 5000)                      │
│                   Route: /medicines/* /configs/*                │
└─────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
┌─────────────────────────────────┐  ┌─────────────────────────────────┐
│    MedicineTrack.Api            │  │  MedicineTrack.Configuration    │
│       (Port 5001)               │  │       (Port 5002)               │
│                                 │  │                                 │
│ • Medication Management         │  │ • Organization Management       │
│ • Medication Logging            │  │ • User Management               │
│ • Drug Interaction Checking     │  │ • System Configuration          │
│ • Medication Database Lookup    │  │                                 │
└─────────────────────────────────┘  └─────────────────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
┌─────────────────────────────────────────────────────────────────┐
│                    Infrastructure                                │
│                                                                 │
│ • PostgreSQL (Medication & Configuration DBs)                  │
│ • Redis/Valkey (Caching)                                       │
│ • Entity Framework Core (Data Access)                          │
│ • Migrations (Database Schema Management)                       │
└─────────────────────────────────────────────────────────────────┘
                                    │
┌─────────────────────────────────────────────────────────────────┐
│                    Telemetry & Observability                     │
│                                                                 │
│ • OpenTelemetry Collector (OTLP: 4317/4318)                    │
│ • RavenDB Ingestion Service (Port 5003)                        │
│ • RavenDB (Telemetry Storage)                                  │
│   - Traces Collection (Application logs)                       │
│   - Requests Collection (HTTP requests)                        │
│   - Dependencies Collection (Database/HTTP calls)              │
│   - Exceptions Collection (Application errors)                 │
└─────────────────────────────────────────────────────────────────┘
```

### Project Structure

```
medicine_track/
├── src/
│   ├── MedicineTrack.AppHost/              # .NET Aspire App Host
│   ├── MedicineTrack.Api/                  # Main API service
│   ├── MedicineTrack.Configuration/        # Configuration service
│   ├── MedicineTrack.Gateway/              # API Gateway (YARP)
│   ├── MedicineTrack.RavenDB.Ingestion/    # Telemetry ingestion service
│   ├── MedicineTrack.Medication.Data/      # Medication domain models
│   ├── MedicineTrack.Configuration.Data/   # Configuration domain models
│   ├── MedicineTrack.Medication.Migrations/    # Database migrations
│   ├── MedicineTrack.Configuration.Migrations/ # Database migrations
│   ├── MedicineTrack.Tests/                # Unit tests
│   └── MedicineTrack.End2EndTests/         # End-to-end tests
├── scripts/
│   ├── validate-ravendb-ingestion.sh       # RavenDB telemetry validation
│   └── ravendb-test-queries.rql            # Example RQL queries
├── otel-collector-config.yaml              # OpenTelemetry Collector config
├── deploy-aspire.sh                        # Kubernetes deployment script
└── README.md
```

## 🚀 Features

### Core Functionality

- **Medication Management**: Create, read, update, and delete medications with detailed information
- **Flexible Scheduling**: Support for complex medication regimens with multiple frequency types
- **Medication Logging**: Track when medications are taken, skipped, or taken as needed
- **Drug Interaction Checking**: Identify potential dangerous medication combinations
- **Medication Database**: Search and lookup medications with NDC codes and detailed information
- **User Management**: Multi-tenant system with organization and user management
- **Health Monitoring**: Built-in health checks and observability

### Scheduling System

Supports various frequency types:
- **Daily**: Once or multiple times per day
- **Weekly**: Specific days of the week
- **Monthly**: Specific days of the month
- **As Needed**: PRN medications
- **Custom Intervals**: Every X days, weeks, or months

### Data Models

#### Medication
- Basic information (name, generic name, brand name, strength, form)
- Physical properties (shape, color)
- Lifecycle management (start date, end date, archival)
- Multiple independent schedules

#### Schedule
- Flexible frequency patterns
- Multiple daily dosing times
- Quantity and unit tracking
- Day-of-week specifications

#### Medication Log
- Timestamp tracking (taken vs. logged)
- Status tracking (taken, skipped, as-needed)
- Actual quantity taken
- Schedule association
- Notes and additional context

## 🛠️ Technology Stack

- **Framework**: .NET 9.0 with ASP.NET Core Minimal APIs
- **Orchestration**: .NET Aspire
- **Database**: PostgreSQL with Entity Framework Core
- **Caching**: Redis/Valkey
- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **Documentation**: OpenAPI/Swagger
- **Testing**: xUnit, End-to-end testing framework
- **Deployment**: Kubernetes with Aspire integration

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Kubernetes cluster](https://kubernetes.io/docs/setup/) (for production deployment)
- [PostgreSQL](https://www.postgresql.org/) (or use Docker container)
- [Redis](https://redis.io/) (or use Docker container)

## 🚀 Quick Start

### Development with .NET Aspire

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd medicine_track
   ```

2. **Install .NET Aspire workload**
   ```bash
   dotnet workload install aspire
   ```

3. **Run the application**
   ```bash
   cd src
   dotnet run --project MedicineTrack.AppHost
   ```

4. **Access the services**
   - Aspire Dashboard: `http://localhost:15888`
   - API Gateway: `http://localhost:5000`
   - Medicine API: `http://localhost:5001`
   - Configuration API: `http://localhost:5002`
   - RavenDB Ingestion: `http://localhost:5003`
   - RavenDB Studio: `https://ravendb.ravendb.orb.local/studio/index.html`

### API Documentation

Once running, access the OpenAPI documentation:
- Gateway API: `http://localhost:5000/openapi`
- Medicine API: `http://localhost:5001/openapi`
- Configuration API: `http://localhost:5002/openapi`

## 📚 API Endpoints

### Medication Management (`/medicines/users/{userId}/medications`)
- `POST /` - Create new medication
- `GET /` - List user medications (with filtering)
- `GET /{medicationId}` - Get specific medication
- `PUT /{medicationId}` - Update medication
- `DELETE /{medicationId}` - Archive medication

### Medication Logging (`/medicines/users/{userId}`)
- `POST /medications/{medicationId}/logs` - Log medication dose
- `GET /medication-logs` - Get all user logs
- `GET /medications/{medicationId}/logs` - Get logs for specific medication
- `PUT /medication-logs/{logId}` - Update log entry
- `DELETE /medication-logs/{logId}` - Delete log entry

### Medication Database (`/medicines/medication-database`)
- `GET /search` - Search medication database

### Drug Interactions (`/medicines/users/{userId}/medication-interactions`)
- `POST /check` - Check for drug interactions

### Organization Management (`/configs/organizations`)
- `POST /` - Create organization
- `GET /` - List organizations
- `GET /{organizationId}` - Get specific organization
- `PUT /{organizationId}` - Update organization
- `DELETE /{organizationId}` - Delete organization

### User Management (`/configs/organizations/{organizationId}/users`)
- `POST /` - Create user
- `GET /` - List users
- `GET /{userId}` - Get specific user
- `PUT /{userId}` - Update user
- `DELETE /{userId}` - Delete user

## 🧪 Testing

### Unit Tests
```bash
cd src
dotnet test MedicineTrack.Tests
```

### End-to-End Tests
```bash
cd src
dotnet test MedicineTrack.End2EndTests
```

### HTTP Testing
Use the provided `.http` files in VS Code with the REST Client extension:
- `src/MedicineTrack.Api/Medicines.http`
- `src/MedicineTrack.Api/Health.http`
- `src/MedicineTrack.Configuration/MedicineTrack.Configuration.http`
- `src/MedicineTrack.Gateway/MedicineTrack.Gateway.http`

## 🚢 Deployment

### Local Kubernetes Deployment

Use the provided deployment script:

```bash
./deploy-aspire.sh
```

This script will:
1. Check prerequisites (kubectl, Docker, .NET SDK)
2. Create Kubernetes namespace
3. Build the application
4. Generate Aspire manifest
5. Convert to Kubernetes manifests using aspirate
6. Deploy to local cluster
7. Set up port forwarding

### Manual Deployment Steps

1. **Build the application**
   ```bash
   cd src
   dotnet build
   ```

2. **Generate Aspire manifest**
   ```bash
   dotnet run --project MedicineTrack.AppHost --publisher manifest --output-path ../infra/aspire-manifest.json
   ```

3. **Install aspirate tool**
   ```bash
   dotnet tool install -g aspirate
   ```

4. **Generate Kubernetes manifests**
   ```bash
   cd infra
   aspirate generate \
     --input-path aspire-manifest.json \
     --output-path k8s-manifests \
     --namespace medicine-track \
     --container-registry docker.io \
     --container-image-tag latest
   ```

5. **Apply to Kubernetes**
   ```bash
   kubectl apply -f k8s-manifests -n medicine-track
   ```

### Environment Variables

Configure the following environment variables for production:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=<PostgreSQL connection string>`
- `ConnectionStrings__Redis=<Redis connection string>`

## 🔧 Configuration

### Database Configuration
The application uses Entity Framework Core with PostgreSQL. Database migrations are handled by dedicated migration projects:
- `MedicineTrack.Medication.Migrations`
- `MedicineTrack.Configuration.Migrations`

### Caching Configuration
Redis/Valkey is used for caching. Configuration is handled through .NET Aspire's service discovery.

### Gateway Configuration
The API Gateway uses YARP for reverse proxy functionality. Routes are configured in `appsettings.json`:
- `/medicines/*` → Medicine API
- `/configs/*` → Configuration API

## 📊 Monitoring and Observability

### Health Checks
- Gateway: `GET /health`
- Medicine API: `GET /health`
- Configuration API: `GET /health`
- RavenDB Ingestion: `GET http://localhost:5003/health`

### Aspire Dashboard
The Aspire dashboard provides comprehensive monitoring:
- Service health and metrics
- Distributed tracing
- Log aggregation
- Resource utilization

Access at: `http://localhost:15888` (development) or via port forwarding (Kubernetes)

### Telemetry with RavenDB

MedicineTrack uses OpenTelemetry and RavenDB for comprehensive telemetry storage and analysis.

#### Architecture

```
Services (Gateway, API, Config)
    │
    │ OTLP (gRPC/HTTP)
    ↓
OpenTelemetry Collector (4317/4318)
    │
    │ File Export / HTTP POST
    ↓
RavenDB Ingestion Service (5003)
    │
    │ RavenDB.Client
    ↓
RavenDB (https://ravendb.ravendb.orb.local)
    └── Database: telemetry
        ├── Traces Collection
        ├── Requests Collection
        ├── Dependencies Collection
        └── Exceptions Collection
```

#### RavenDB Setup

1. **Prerequisites**
   - RavenDB instance running at `https://ravendb.ravendb.orb.local`
   - Database named `telemetry` created in RavenDB Studio

2. **Configuration**

   The RavenDB connection can be configured via environment variables or `appsettings.json`:

   ```json
   {
     "RavenDB": {
       "Url": "https://ravendb.ravendb.orb.local",
       "Database": "telemetry"
     }
   }
   ```

   Or via environment variables:
   ```bash
   export RavenDB__Url="https://ravendb.ravendb.orb.local"
   export RavenDB__Database="telemetry"
   ```

3. **Accessing RavenDB Studio**

   Open RavenDB Studio to query and visualize telemetry data:
   ```
   https://ravendb.ravendb.orb.local/studio/index.html
   ```

#### Telemetry Collections

**Traces Collection** - Application logs and structured traces
```rql
from Traces
where Timestamp > @now.AddMinutes(-10)
order by Timestamp desc
```

**Requests Collection** - HTTP requests to services
```rql
from Requests
where Success == false
order by Timestamp desc
```

**Dependencies Collection** - External calls (database, HTTP)
```rql
from Dependencies
where DurationMs > 100
order by DurationMs desc
```

**Exceptions Collection** - Application errors
```rql
from Exceptions
group by InnermostType
select InnermostType, count() as ErrorCount
order by ErrorCount desc
```

#### Distributed Tracing

Query across services using OperationId (trace ID):
```rql
from index 'Telemetry/ByTraceId'
where OperationId == "your-trace-id-here"
order by Timestamp asc
```

This returns all telemetry (traces, requests, dependencies) for a single request across all services.

#### Query Examples

See `scripts/ravendb-test-queries.rql` for comprehensive query examples:
- Recent traces (health check)
- Request volume by service
- Slow dependencies
- Exception summary
- Error rate by service
- Request duration percentiles
- Service-to-service call map

#### Validation

Run the validation script to test telemetry ingestion:
```bash
bash scripts/validate-ravendb-ingestion.sh
```

This script:
1. Tests RavenDB connection
2. Verifies collection existence
3. Generates test traffic
4. Validates data ingestion
5. Checks schema compliance
6. Tests distributed tracing
7. Verifies custom indexes

#### Custom Indexes

The system includes custom RavenDB indexes for optimized queries:

- **Traces/ByServiceAndTime** - Filter traces by service and time range
- **Requests/ByServiceAndStatus** - Analyze request success rates
- **Telemetry/ByTraceId** - Multi-map index for distributed tracing

Indexes are automatically deployed on service startup.

#### Troubleshooting

**Connection Issues**
```bash
# Test RavenDB connection
curl -k https://ravendb.ravendb.orb.local/databases/telemetry/stats

# Check ingestion service health
curl http://localhost:5003/health
```

**No Data in RavenDB**
- Verify OTEL Collector is running (check Aspire dashboard)
- Check RavenDB Ingestion service logs for errors
- Ensure 'telemetry' database exists in RavenDB Studio
- Collections are created automatically on first insert

**Query Performance**
- Check index status in RavenDB Studio (Databases → telemetry → Indexes)
- Indexes build asynchronously - wait a few moments after service start
- Use index queries (`from index 'IndexName'`) for better performance

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Use nullable reference types
- Write unit tests for new features
- Update API documentation
- Ensure all tests pass before submitting

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For questions and support:
- Create an issue in the GitHub repository
- Check the API documentation for endpoint details
- Review the test files for usage examples

## 🎯 Roadmap

- [ ] Authentication and authorization
- [ ] Real-time notifications
- [ ] Mobile app integration
- [ ] Advanced analytics and reporting
- [ ] Integration with pharmacy systems
- [ ] Machine learning for adherence predictions
- [ ] Multi-language support
- [ ] Offline capability

---

Built with ❤️ using .NET Aspire and modern cloud-native technologies.

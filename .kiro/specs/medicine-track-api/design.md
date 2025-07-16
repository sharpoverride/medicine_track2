# Design Document

## Overview

The Medicine Track API is designed as a RESTful web service built on ASP.NET Core 9.0 using minimal APIs. The system follows a clean architecture approach with clear separation of concerns, focusing on medication management, scheduling, logging, and safety features like drug interaction checking.

The API serves as the backend for a personal medication tracking application, providing endpoints for CRUD operations on medications, flexible scheduling systems, comprehensive logging capabilities, and integration with medication databases for safety and accuracy.

## Architecture

### High-Level Architecture

The system follows a layered architecture pattern:

```
┌─────────────────────────────────────┐
│           Client Applications       │
├─────────────────────────────────────┤
│              HTTP/HTTPS             │
├─────────────────────────────────────┤
│            API Gateway              │
│         (Future Enhancement)        │
├─────────────────────────────────────┤
│          Medicine Track API         │
│    ┌─────────────────────────────┐  │
│    │     Presentation Layer      │  │
│    │   (Minimal API Endpoints)   │  │
│    ├─────────────────────────────┤  │
│    │      Business Logic        │  │
│    │    (Service Layer)         │  │
│    ├─────────────────────────────┤  │
│    │      Data Access Layer     │  │
│    │   (Repository Pattern)     │  │
│    └─────────────────────────────┘  │
├─────────────────────────────────────┤
│           Data Storage              │
│    ┌─────────────┬─────────────┐    │
│    │  Database   │   External  │    │
│    │ (Future)    │  Services   │    │
│    └─────────────┴─────────────┘    │
└─────────────────────────────────────┘
```

### Technology Stack

- **Framework**: ASP.NET Core 9.0 with Minimal APIs
- **Language**: C# with nullable reference types enabled
- **Serialization**: System.Text.Json with enum string conversion
- **Documentation**: OpenAPI/Swagger integration
- **Logging**: Built-in ASP.NET Core logging with custom middleware
- **Hosting**: Aspire-compatible for cloud deployment

### Current Implementation Approach

The current implementation uses in-memory data structures with placeholder logic, designed to be easily replaceable with actual database implementations. This approach allows for rapid prototyping and testing while maintaining the correct API contracts.

## Components and Interfaces

### Core Domain Models

#### User
```csharp
public record User(
    Guid Id, 
    string Email, 
    string Name, 
    string Timezone, 
    DateTimeOffset CreatedAt, 
    DateTimeOffset UpdatedAt
);
```

#### Medication
```csharp
public record Medication(
    Guid Id,
    Guid UserId,
    string Name,
    string? GenericName,
    string? BrandName,
    string Strength,
    string Form,
    string? Shape,
    string? Color,
    string? Notes,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsArchived,
    List<Schedule> Schedules,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
```

#### Schedule
```csharp
public record Schedule(
    Guid Id,
    FrequencyType FrequencyType,
    int? Interval,
    List<DayOfWeek>? DaysOfWeek,
    List<TimeOnly> TimesOfDay,
    double? Quantity,
    string? Unit
);
```

#### MedicationLog
```csharp
public record MedicationLog(
    Guid Id,
    Guid UserId,
    Guid MedicationId,
    Guid? ScheduleId,
    DateTimeOffset TakenAt,
    LogStatus Status,
    double? QuantityTaken,
    string? Notes,
    DateTimeOffset LoggedAt
);
```

### API Endpoint Groups

#### 1. Medication Management (`/users/{userId}/medications`)
- **POST** `/` - Create new medication
- **GET** `/` - List user medications with filtering
- **GET** `/{medicationId}` - Get specific medication
- **PUT** `/{medicationId}` - Update medication
- **DELETE** `/{medicationId}` - Archive medication

#### 2. Medication Logging (`/users/{userId}`)
- **POST** `/medications/{medicationId}/logs` - Log medication dose
- **GET** `/medication-logs` - Get all user logs with filtering
- **GET** `/medications/{medicationId}/logs` - Get logs for specific medication
- **PUT** `/medication-logs/{logId}` - Update log entry
- **DELETE** `/medication-logs/{logId}` - Delete log entry

#### 3. Medication Database (`/medication-database`)
- **GET** `/search` - Search medication database

#### 4. Drug Interactions (`/users/{userId}/medication-interactions`)
- **POST** `/check` - Check for drug interactions

#### 5. System Health (`/health`)
- **GET** `/` - Health check endpoint

### Request/Response DTOs

The API uses separate Data Transfer Objects for requests to provide clean interfaces and validation:

- `CreateMedicationRequest` / `UpdateMedicationRequest`
- `CreateScheduleRequest`
- `LogMedicationRequest` / `UpdateMedicationLogRequest`
- `CheckInteractionRequest`

### Enumerations

```csharp
public enum FrequencyType { 
    DAILY, WEEKLY, MONTHLY, AS_NEEDED, 
    EVERY_X_DAYS, SPECIFIC_DAYS_OF_WEEK, 
    EVERY_X_WEEKS, EVERY_X_MONTHS 
}

public enum LogStatus { 
    TAKEN, SKIPPED, LOGGED_AS_NEEDED 
}

public enum InteractionSeverity { 
    HIGH, MODERATE, LOW, UNKNOWN 
}
```

## Data Models

### Medication Scheduling System

The scheduling system supports complex medication regimens through a flexible `Schedule` model:

1. **Frequency Types**: Multiple patterns including daily, weekly, monthly, as-needed, and custom intervals
2. **Time Specification**: Multiple times per day using `TimeOnly` for precision
3. **Dosage Information**: Quantity and unit tracking for accurate dosing
4. **Multiple Schedules**: Each medication can have multiple independent schedules

### Logging System

The logging system provides comprehensive tracking:

1. **Temporal Tracking**: Separate timestamps for when medication was taken vs. when it was logged
2. **Status Tracking**: Taken, skipped, or as-needed logging
3. **Quantity Tracking**: Actual quantity taken vs. scheduled quantity
4. **Schedule Association**: Optional linking to specific schedules
5. **Notes**: Free-form text for additional context

### Drug Interaction System

The interaction checking system supports:

1. **Multiple Medication Analysis**: Check interactions between existing medications
2. **New Medication Screening**: Check new medications against existing regimen
3. **Severity Classification**: High, moderate, low, and unknown severity levels
4. **Source Attribution**: Track where interaction data comes from
5. **Detailed Descriptions**: Human-readable interaction explanations

## Error Handling

### HTTP Status Code Strategy

- **200 OK**: Successful GET and PUT operations
- **201 Created**: Successful POST operations with resource creation
- **204 No Content**: Successful DELETE operations
- **400 Bad Request**: Invalid request data or missing required fields
- **404 Not Found**: Requested resource doesn't exist
- **500 Internal Server Error**: Unexpected server errors

### Validation Strategy

1. **Model Validation**: Automatic validation through ASP.NET Core model binding
2. **Business Rule Validation**: Custom validation logic in service layer
3. **Data Consistency**: Validation of relationships between entities
4. **Input Sanitization**: Protection against malicious input

### Error Response Format

Consistent error responses following problem details specification:
```json
{
  "type": "https://example.com/problems/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": {
    "Name": ["The Name field is required."]
  }
}
```

## Testing Strategy

### Unit Testing Approach

1. **Model Testing**: Validate record types and their behavior
2. **Endpoint Testing**: Test API endpoints with various inputs
3. **Business Logic Testing**: Test scheduling algorithms and interaction logic
4. **Validation Testing**: Test input validation and error handling

### Integration Testing Approach

1. **End-to-End API Testing**: Full request/response cycle testing
2. **Database Integration**: Test data persistence and retrieval
3. **External Service Integration**: Test medication database and interaction services
4. **Authentication Integration**: Test user isolation and security

### Test Data Strategy

1. **Fixture Data**: Consistent test data for repeatable tests
2. **Edge Case Testing**: Boundary conditions and error scenarios
3. **Performance Testing**: Load testing for scalability validation
4. **Security Testing**: Input validation and injection attack prevention

### Testing Tools and Frameworks

- **xUnit**: Primary testing framework
- **ASP.NET Core Test Host**: In-memory testing
- **Moq**: Mocking framework for dependencies
- **FluentAssertions**: Readable test assertions
- **WebApplicationFactory**: Integration testing support

## Security Considerations

### Data Protection

1. **User Data Isolation**: All operations scoped to specific user IDs
2. **Input Validation**: Comprehensive validation of all inputs
3. **SQL Injection Prevention**: Parameterized queries (when database is implemented)
4. **XSS Prevention**: Proper output encoding

### Authentication and Authorization

1. **User Authentication**: JWT or similar token-based authentication (future implementation)
2. **Authorization Policies**: Role-based access control
3. **API Key Management**: Secure API key handling for external services
4. **Rate Limiting**: Protection against abuse

### HTTPS and Transport Security

1. **TLS Encryption**: All communications over HTTPS
2. **HSTS Headers**: HTTP Strict Transport Security
3. **Secure Headers**: Comprehensive security header implementation
4. **CORS Configuration**: Proper cross-origin resource sharing setup

## Performance Considerations

### Caching Strategy

1. **Medication Database Caching**: Cache frequently accessed medication information
2. **User Data Caching**: Cache user-specific data with appropriate TTL
3. **Interaction Data Caching**: Cache interaction check results
4. **Response Caching**: HTTP response caching for static data

### Database Optimization

1. **Indexing Strategy**: Proper indexing on frequently queried fields
2. **Query Optimization**: Efficient database queries
3. **Connection Pooling**: Optimal database connection management
4. **Pagination**: Implement pagination for large result sets

### API Performance

1. **Response Compression**: Gzip compression for responses
2. **Minimal Data Transfer**: Only return necessary data
3. **Async Operations**: Non-blocking I/O operations
4. **Resource Pooling**: Efficient resource utilization

## Deployment and Infrastructure

### Aspire Integration

The application is designed for deployment using .NET Aspire:

1. **Service Discovery**: Automatic service registration and discovery
2. **Configuration Management**: Centralized configuration
3. **Observability**: Built-in logging, metrics, and tracing
4. **Health Checks**: Comprehensive health monitoring

### Scalability Design

1. **Stateless Design**: No server-side session state
2. **Horizontal Scaling**: Support for multiple API instances
3. **Database Scaling**: Read replicas and connection pooling
4. **Load Balancing**: Support for load balancer integration

### Monitoring and Observability

1. **Application Logging**: Comprehensive logging with structured data
2. **Performance Metrics**: Key performance indicators tracking
3. **Health Monitoring**: Endpoint health checks
4. **Error Tracking**: Centralized error logging and alerting

## Future Enhancements

### Database Implementation

1. **Entity Framework Core**: ORM implementation for data persistence
2. **Database Migrations**: Version-controlled schema changes
3. **Data Seeding**: Initial data population strategies
4. **Backup and Recovery**: Data protection strategies

### Advanced Features

1. **Medication Reminders**: Push notification system
2. **Adherence Analytics**: Medication compliance reporting
3. **Healthcare Provider Integration**: Sharing data with medical professionals
4. **Mobile App Support**: Optimized endpoints for mobile applications

### External Integrations

1. **Pharmacy APIs**: Integration with pharmacy systems
2. **EHR Integration**: Electronic health record connectivity
3. **Insurance APIs**: Medication coverage checking
4. **Clinical Decision Support**: Advanced interaction checking
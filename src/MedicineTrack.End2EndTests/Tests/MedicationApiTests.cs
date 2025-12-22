using System.Text;
using System.Text.Json;
using MedicineTrack.End2EndTests.Fixtures;
using Xunit;

namespace MedicineTrack.End2EndTests.Tests;

/// <summary>
/// E2E tests for the Medication API.
/// Uses ICollectionFixture for xUnit (Rider/VS) and supports the custom E2E Runner.
/// </summary>
[Collection("E2ETests")]
public class MedicationApiTests
{
    private readonly HttpClient _medicationHttpClient;
    private readonly SystemUserFixture _systemUserFixture;
    private readonly ILogger<MedicationApiTests> _logger;

    public MedicationApiTests(TestServicesFixture fixture)
    {
        _medicationHttpClient = fixture.HttpClientFactory.CreateClient("medicine-track-api");
        _systemUserFixture = fixture.SystemUserFixture;
        _logger = fixture.GetLogger<MedicationApiTests>();
    }

    [Fact]
    public async Task HealthCheck_Should_Return_OK()
    {
        // Act
        var response = await _medicationHttpClient.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Health check response: {Content}", content);
        Assert.Contains("MedicineTrack API is OK", content);
    }

    [Fact]
    public async Task CreateMedication_Should_Return_Created()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var createRequest = new
        {
            Name = "Test Medication",
            GenericName = "TestGeneric",
            BrandName = "TestBrand",
            Strength = "10 mg",  // Note: space required between number and unit
            Form = "Tablet",
            Shape = "Round",
            Color = "White",
            Notes = "Test medication for E2E testing",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = (DateOnly?)null,
            Schedules = new[]
            {
                new
                {
                    FrequencyType = "EVERY_X_DAYS",
                    Interval = 1,
                    DaysOfWeek = (string[]?)null,
                    TimesOfDay = new[] { "08:00" },
                    Quantity = 1.0,
                    Unit = "tablet"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PostAsync($"/users/{userId}/medications", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Created medication response: {Content}", responseContent);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetMedications_Should_Return_List()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;

        // Act
        var response = await _medicationHttpClient.GetAsync($"/users/{userId}/medications");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get medications response: {Content}", content);
        
        // Should return an array (even if empty)
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Theory]
    [InlineData("active")]
    [InlineData("archived")]
    public async Task GetMedications_WithStatus_Should_Return_List(string status)
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;

        // Act
        var response = await _medicationHttpClient.GetAsync($"/users/{userId}/medications?status={status}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get medications with status {Status} response: {Content}", status, content);
        
        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Fact]
    public async Task GetMedication_Should_Return_Medication()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var medicationId = Guid.NewGuid();

        // Act
        var response = await _medicationHttpClient.GetAsync($"/users/{userId}/medications/{medicationId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get medication response: {Content}", content);
        
        // Should return a medication object
        Assert.StartsWith("{", content.Trim());
        Assert.EndsWith("}", content.Trim());
    }

    [Fact]
    public async Task UpdateMedication_Should_Return_Updated()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var medicationId = Guid.NewGuid();
        var updateRequest = new
        {
            Name = "Updated Medication",
            Notes = "Updated notes",
            IsArchived = false
        };

        var json = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PutAsync($"/users/{userId}/medications/{medicationId}", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Updated medication response: {Content}", responseContent);
        
        // Should return updated medication object
        Assert.StartsWith("{", responseContent.Trim());
        Assert.EndsWith("}", responseContent.Trim());
    }

    [Fact]
    public async Task DeleteMedication_Should_Return_NoContent()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var medicationId = Guid.NewGuid();

        // Act
        var response = await _medicationHttpClient.DeleteAsync($"/users/{userId}/medications/{medicationId}");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        
        _logger.LogInformation("Deleted medication, status: {StatusCode}", response.StatusCode);
    }

    [Fact]
    public async Task SearchMedicationDatabase_Should_Return_Results()
    {
        // Arrange
        var query = "Lisinopril";

        // Act
        var response = await _medicationHttpClient.GetAsync($"/medication-database/search?query={query}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Search medication database response: {Content}", content);
        
        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Fact]
    public async Task LogMedication_Should_Return_Created()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var medicationId = Guid.NewGuid();
        var logRequest = new
        {
            ScheduleId = Guid.NewGuid(),
            TakenAt = DateTimeOffset.UtcNow,
            Status = "TAKEN",
            QuantityTaken = 1.0,
            Notes = "Taken with breakfast"
        };

        var json = JsonSerializer.Serialize(logRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PostAsync($"/users/{userId}/medications/{medicationId}/logs", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Log medication response: {Content}", responseContent);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetMedicationLogs_Should_Return_List()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;

        // Act
        var response = await _medicationHttpClient.GetAsync($"/users/{userId}/medication-logs");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Get medication logs response: {Content}", content);

        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Theory]
    [InlineData("TAKEN")]
    [InlineData("SKIPPED")]
    public async Task GetMedicationLogs_WithStatus_Should_Return_List(string status)
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;

        // Act
        var response = await _medicationHttpClient.GetAsync($"/users/{userId}/medication-logs?status={status}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Get medication logs with status {Status} response: {Content}", status, content);

        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Fact]
    public async Task GetMedicationLogs_ForSpecificMedication_Should_Return_List()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var medicationId = Guid.NewGuid();

        // Act
        var response = await _medicationHttpClient.GetAsync($"/users/{userId}/medications/{medicationId}/logs");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Get logs for medication {MedicationId} response: {Content}", medicationId, content);

        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Fact]
    public async Task UpdateMedicationLog_Should_Return_Updated()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var logId = Guid.NewGuid();
        var updateRequest = new
        {
            TakenAt = DateTimeOffset.UtcNow,
            Status = "TAKEN",
            QuantityTaken = 2.0,
            Notes = "Updated log notes"
        };

        var json = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PutAsync($"/users/{userId}/medication-logs/{logId}", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Updated medication log response: {Content}", responseContent);

        // Should return updated log object
        Assert.StartsWith("{", responseContent.Trim());
        Assert.EndsWith("}", responseContent.Trim());
    }

    [Fact]
    public async Task DeleteMedicationLog_Should_Return_NoContent()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var logId = Guid.NewGuid();

        // Act
        var response = await _medicationHttpClient.DeleteAsync($"/users/{userId}/medication-logs/{logId}");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

        _logger.LogInformation("Deleted medication log, status: {StatusCode}", response.StatusCode);
    }

    [Fact]
    public async Task CheckMedicationInteractions_Should_Return_Warnings()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var checkRequest = new
        {
            MedicationIds = new[] { Guid.NewGuid(), Guid.NewGuid() },
            NewMedication = (object?)null,
            ExistingMedicationIds = (Guid[]?)null
        };

        var json = JsonSerializer.Serialize(checkRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PostAsync($"/users/{userId}/medication-interactions/check", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Check medication interactions response: {Content}", responseContent);

        // Should return an array of warnings
        Assert.StartsWith("[", responseContent.Trim());
        Assert.EndsWith("]", responseContent.Trim());
    }

    [Fact]
    public async Task CheckMedicationInteractions_WithNewMedication_Should_Return_Warnings()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var checkRequest = new
        {
            MedicationIds = (Guid[]?)null,
            NewMedication = new
            {
                NdcCode = "12345-678-90",
                Name = "New Test Medication",
                GenericName = "TestGeneric",
                BrandNames = new[] { "TestBrand" },
                AvailableForms = new[] { "Tablet" },
                AvailableStrengths = new[] { "10mg" },
                Manufacturer = "Test Pharma"
            },
            ExistingMedicationIds = new[] { Guid.NewGuid() }
        };

        var json = JsonSerializer.Serialize(checkRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PostAsync($"/users/{userId}/medication-interactions/check", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Check interactions with new medication response: {Content}", responseContent);

        // Should return an array of warnings
        Assert.StartsWith("[", responseContent.Trim());
        Assert.EndsWith("]", responseContent.Trim());
    }

    #region Validation and Error Scenarios

    [Fact]
    public async Task CreateMedication_WithInvalidStrength_Should_Return_BadRequest()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var createRequest = new
        {
            Name = "Test Medication",
            Strength = "invalid", // Invalid format - should be like "10 mg" (with space)
            Form = "Tablet",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Schedules = new[]
            {
                new
                {
                    FrequencyType = "DAILY",
                    TimesOfDay = new[] { "08:00" },
                    Quantity = 1.0,
                    Unit = "tablet"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PostAsync($"/users/{userId}/medications", content);

        // Assert
        _logger.LogInformation("Create medication with invalid strength response: {StatusCode}", response.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateMedication_WithEmptyName_Should_Return_BadRequest()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var createRequest = new
        {
            Name = "", // Empty name should fail validation
            Strength = "10 mg",
            Form = "Tablet",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Schedules = new[]
            {
                new
                {
                    FrequencyType = "DAILY",
                    TimesOfDay = new[] { "08:00" },
                    Quantity = 1.0,
                    Unit = "tablet"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PostAsync($"/users/{userId}/medications", content);

        // Assert
        _logger.LogInformation("Create medication with empty name response: {StatusCode}", response.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateMedication_WithNoSchedules_Should_Return_BadRequest()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var createRequest = new
        {
            Name = "Test Medication",
            Strength = "10 mg",
            Form = "Tablet",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Schedules = Array.Empty<object>() // Empty schedules should fail validation
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PostAsync($"/users/{userId}/medications", content);

        // Assert
        _logger.LogInformation("Create medication with no schedules response: {StatusCode}", response.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateMedication_WithEndDateBeforeStartDate_Should_Return_BadRequest()
    {
        // Arrange
        var userId = _systemUserFixture.SystemUserId;
        var createRequest = new
        {
            Name = "Test Medication",
            Strength = "10 mg",
            Form = "Tablet",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow), // End before start
            Schedules = new[]
            {
                new
                {
                    FrequencyType = "DAILY",
                    TimesOfDay = new[] { "08:00" },
                    Quantity = 1.0,
                    Unit = "tablet"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _medicationHttpClient.PostAsync($"/users/{userId}/medications", content);

        // Assert
        _logger.LogInformation("Create medication with end date before start date response: {StatusCode}", response.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchMedicationDatabase_WithEmptyQuery_Should_Return_EmptyArray()
    {
        // Arrange - empty query should still return valid response

        // Act
        var response = await _medicationHttpClient.GetAsync("/medication-database/search?query=");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Search with empty query response: {Content}", content);

        // Should return an array (possibly empty)
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    #endregion
}

using System.Text;
using System.Text.Json;
using MedicineTrack.End2EndTests.Fixtures;
using Xunit;

namespace MedicineTrack.End2EndTests.Tests;

public class MedicationApiTests : IAsyncLifetime
{
    private readonly HttpClient _medicationHttpClient;
    private readonly SystemUserFixture _systemUserFixture;
    private readonly ILogger<MedicationApiTests> _logger;

    public MedicationApiTests(
        IHttpClientFactory httpClientFactory,
        SystemUserFixture systemUserFixture,
        ILogger<MedicationApiTests> logger)
    {
        _medicationHttpClient = httpClientFactory.CreateClient("medicine-track-api");
        _systemUserFixture = systemUserFixture;
        _logger = logger;
    }

    public async ValueTask InitializeAsync()
    {
        // SystemUserFixture is already initialized by xUnit when used as IClassFixture
        // For our custom scheduler, we ensure it's initialized here
        if (_systemUserFixture.SystemUserId == Guid.Empty)
        {
            await _systemUserFixture.InitializeAsync();
        }
    }

    public ValueTask DisposeAsync()
    {
        // Let xUnit handle disposal when used as IClassFixture
        // For our custom scheduler, we handle disposal here
        return ValueTask.CompletedTask;
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
            Strength = "10mg",
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
}

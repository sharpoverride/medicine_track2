using System.Text;
using System.Text.Json;
using MedicineTrack.End2EndTests.Fixtures;
using Xunit;

namespace MedicineTrack.End2EndTests.Tests;

public class ConfigurationApiTests : IClassFixture<SystemUserFixture>
{
    private readonly HttpClient _configHttpClient;
    private readonly SystemUserFixture _systemUserFixture;
    private readonly ILogger<ConfigurationApiTests> _logger;

    public ConfigurationApiTests(
        IHttpClientFactory httpClientFactory,
        SystemUserFixture systemUserFixture,
        ILogger<ConfigurationApiTests> logger)
    {
        _configHttpClient = httpClientFactory.CreateClient("medicine-track-config");
        _systemUserFixture = systemUserFixture;
        _logger = logger;
    }

    [Fact]
    public async Task HealthCheck_Should_Return_OK()
    {
        // Act
        var response = await _configHttpClient.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Health check response: {Content}", content);
        Assert.Contains("Configuration API is healthy", content);
    }

    [Fact]
    public async Task GetOrganizations_Should_Return_List()
    {
        // Act
        var response = await _configHttpClient.GetAsync("/organizations");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get organizations response: {Content}", content);
        
        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Theory]
    [InlineData("healthcare")]
    [InlineData("medical")]
    public async Task GetOrganizations_WithSearch_Should_Return_List(string search)
    {
        // Act
        var response = await _configHttpClient.GetAsync($"/organizations?search={search}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get organizations with search '{Search}' response: {Content}", search, content);
        
        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Fact]
    public async Task GetOrganization_Should_Return_Organization()
    {
        // Arrange
        var organizationId = _systemUserFixture.OrganizationId;

        // Act
        var response = await _configHttpClient.GetAsync($"/organizations/{organizationId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get organization response: {Content}", content);
        
        // Should return an organization object
        Assert.StartsWith("{", content.Trim());
        Assert.EndsWith("}", content.Trim());
    }

    [Fact]
    public async Task UpdateOrganization_Should_Return_Updated()
    {
        // Arrange
        var organizationId = _systemUserFixture.OrganizationId;
        var updateRequest = new
        {
            Name = "Updated Test Organization",
            Description = "Updated description for testing",
            IsActive = true
        };

        var json = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _configHttpClient.PutAsync($"/organizations/{organizationId}", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Updated organization response: {Content}", responseContent);
        
        // Should return updated organization object
        Assert.StartsWith("{", responseContent.Trim());
        Assert.EndsWith("}", responseContent.Trim());
    }

    [Fact]
    public async Task GetUsers_Should_Return_List()
    {
        // Arrange
        var organizationId = _systemUserFixture.OrganizationId;

        // Act
        var response = await _configHttpClient.GetAsync($"/organizations/{organizationId}/users");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get users response: {Content}", content);
        
        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Theory]
    [InlineData("USER")]
    [InlineData("ADMIN")]
    public async Task GetUsers_WithRole_Should_Return_List(string role)
    {
        // Arrange
        var organizationId = _systemUserFixture.OrganizationId;

        // Act
        var response = await _configHttpClient.GetAsync($"/organizations/{organizationId}/users?role={role}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get users with role '{Role}' response: {Content}", role, content);
        
        // Should return an array
        Assert.StartsWith("[", content.Trim());
        Assert.EndsWith("]", content.Trim());
    }

    [Fact]
    public async Task GetUser_Should_Return_User()
    {
        // Arrange
        var organizationId = _systemUserFixture.OrganizationId;
        var userId = _systemUserFixture.SystemUserId;

        // Act
        var response = await _configHttpClient.GetAsync($"/organizations/{organizationId}/users/{userId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Get user response: {Content}", content);
        
        // Should return a user object
        Assert.StartsWith("{", content.Trim());
        Assert.EndsWith("}", content.Trim());
    }

    [Fact]
    public async Task UpdateUser_Should_Return_Updated()
    {
        // Arrange
        var organizationId = _systemUserFixture.OrganizationId;
        var userId = _systemUserFixture.SystemUserId;
        var updateRequest = new
        {
            Name = "Updated System User",
            PhoneNumber = "+1-555-UPDATED",
            Timezone = "America/New_York",
            IsActive = true
        };

        var json = JsonSerializer.Serialize(updateRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _configHttpClient.PutAsync($"/organizations/{organizationId}/users/{userId}", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Updated user response: {Content}", responseContent);
        
        // Should return updated user object
        Assert.StartsWith("{", responseContent.Trim());
        Assert.EndsWith("}", responseContent.Trim());
    }

    [Fact]
    public async Task CreateTemporaryUser_Should_Return_Created()
    {
        // Arrange
        var organizationId = _systemUserFixture.OrganizationId;
        var createRequest = new
        {
            Email = "temp@medicinetrack.test",
            Name = "Temporary Test User",
            Role = "USER",
            PhoneNumber = "+1-555-TEMP",
            Timezone = "UTC"
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _configHttpClient.PostAsync($"/organizations/{organizationId}/users", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Created temporary user response: {Content}", responseContent);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        // Clean up - extract user ID and delete
        try
        {
            var userResponse = JsonSerializer.Deserialize<UserResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (userResponse != null)
            {
                var deleteResponse = await _configHttpClient.DeleteAsync($"/organizations/{organizationId}/users/{userResponse.Id}");
                _logger.LogInformation("Cleaned up temporary user: {UserId}", userResponse.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temporary user");
        }
    }

    [Fact]
    public async Task CreateTemporaryOrganization_Should_Return_Created()
    {
        // Arrange
        var createRequest = new
        {
            Name = "Temporary Test Organization",
            Description = "Organization for temporary testing",
            ContactEmail = "temp@medicinetrack.test",
            Address = "456 Temp St, Temp City, TC 67890",
            PhoneNumber = "+1-555-TEMP"
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _configHttpClient.PostAsync("/organizations", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("Created temporary organization response: {Content}", responseContent);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        // Clean up - extract organization ID and delete
        try
        {
            var orgResponse = JsonSerializer.Deserialize<OrganizationResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (orgResponse != null)
            {
                var deleteResponse = await _configHttpClient.DeleteAsync($"/organizations/{orgResponse.Id}");
                _logger.LogInformation("Cleaned up temporary organization: {OrganizationId}", orgResponse.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temporary organization");
        }
    }
}

using System.Text.Json;
using Xunit;

namespace MedicineTrack.End2EndTests.Fixtures;

public class SystemUserFixture : IAsyncLifetime
{
    private readonly HttpClient _configHttpClient;
    private readonly ILogger<SystemUserFixture> _logger;

    public Guid OrganizationId { get; private set; }
    public Guid SystemUserId { get; private set; }
    public string SystemUserEmail { get; private set; } = "system@medicinetrack.test";
    public string SystemUserName { get; private set; } = "System Test User";

    public SystemUserFixture(IHttpClientFactory httpClientFactory, ILogger<SystemUserFixture> logger)
    {
        _configHttpClient = httpClientFactory.CreateClient("medicine-track-config");
        _logger = logger;
    }

    public async ValueTask InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing system user fixture...");
            
            // Create organization first
            await CreateOrganizationAsync();
            
            // Create system user
            await CreateSystemUserAsync();
            
            _logger.LogInformation($"System user initialized - OrgId: {OrganizationId}, UserId: {SystemUserId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system user fixture");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _logger.LogInformation("Cleaning up system user fixture...");
            
            // Clean up system user
            if (SystemUserId != Guid.Empty)
            {
                await DeleteSystemUserAsync();
            }
            
            // Clean up organization
            if (OrganizationId != Guid.Empty)
            {
                await DeleteOrganizationAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up system user fixture");
        }
    }

    private async Task CreateOrganizationAsync()
    {
        var createOrgRequest = new
        {
            Name = "Test Organization",
            Description = "Organization for end-to-end testing",
            ContactEmail = "admin@medicinetrack.test",
            Address = "123 Test St, Test City, TC 12345",
            PhoneNumber = "+1-555-TEST"
        };

        var json = JsonSerializer.Serialize(createOrgRequest);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _configHttpClient.PostAsync("/organizations", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var organization = JsonSerializer.Deserialize<OrganizationResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        OrganizationId = organization!.Id;
        _logger.LogInformation($"Created test organization with ID: {OrganizationId}");
    }

    private async Task CreateSystemUserAsync()
    {
        var createUserRequest = new
        {
            Email = SystemUserEmail,
            Name = SystemUserName,
            Role = "USER",
            PhoneNumber = "+1-555-SYSTEM",
            Timezone = "UTC"
        };

        var json = JsonSerializer.Serialize(createUserRequest);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _configHttpClient.PostAsync($"/organizations/{OrganizationId}/users", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        SystemUserId = user!.Id;
        _logger.LogInformation($"Created system user with ID: {SystemUserId}");
    }

    private async Task DeleteSystemUserAsync()
    {
        try
        {
            var response = await _configHttpClient.DeleteAsync($"/organizations/{OrganizationId}/users/{SystemUserId}");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Deleted system user: {SystemUserId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to delete system user: {SystemUserId}");
        }
    }

    private async Task DeleteOrganizationAsync()
    {
        try
        {
            var response = await _configHttpClient.DeleteAsync($"/organizations/{OrganizationId}");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Deleted organization: {OrganizationId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to delete organization: {OrganizationId}");
        }
    }
}

public record OrganizationResponse(
    Guid Id,
    string Name,
    string? Description,
    string ContactEmail,
    string? Address,
    string? PhoneNumber,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record UserResponse(
    Guid Id,
    Guid OrganizationId,
    string Email,
    string Name,
    string Role,
    string? PhoneNumber,
    string Timezone,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

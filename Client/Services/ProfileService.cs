using System.Net.Http.Json;
using SkillSnap.Shared.Models;
using System.Text.Json;

namespace SkillSnap.Client.Services;

public class ProfileService
{
    private readonly HttpClient _httpClient;

    public ProfileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PortfolioUser?> GetProfileAsync()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return await _httpClient.GetFromJsonAsync<PortfolioUser>("api/users/profile", options);
    }
}
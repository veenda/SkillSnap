using SkillSnap.Shared.Models;
using System.Net.Http.Json;

public class SkillService
{
    private readonly HttpClient _httpClient;

    public SkillService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Skill>> GetSkillsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Skill>>("api/skills") ?? new List<Skill>();
    }

    public async Task<HttpResponseMessage> AddSkillAsync(Skill newSkill)
    {
        var response = await _httpClient.PostAsJsonAsync("api/skills", newSkill);
        return response;
    }
}


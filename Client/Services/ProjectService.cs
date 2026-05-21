using SkillSnap.Shared.Models;
using System.Net.Http.Json;

public class ProjectService
{
    private readonly HttpClient _httpClient;
    public ProjectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Project>>("api/projects");
    }

    public async Task AddProject(Project newProject)
    {
        await _httpClient.PostAsJsonAsync("api/projects", newProject);
    }
}
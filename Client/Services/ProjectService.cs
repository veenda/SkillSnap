using SkillSnap.Shared.Models;
using System.Net.Http.Json;

// This service should use HttpClient to:
// - GetProjectAsync()
// - AddProjectAsync(Project newProject)

public class ProjectService
{
    private readonly HttpClient _httpClient;
    public ProjectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Project>>("api/projects") ?? new List<Project>();
    }

    public async Task AddProject(Project newProject)
    {
        await _httpClient.PostAsJsonAsync("api/projects", newProject);
    }
}
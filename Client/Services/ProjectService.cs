using SkillSnap.Shared.Models;
using System.Net.Http.Json;

namespace SkillSnap.Client.Services;

public class ProjectService : IApiService<Project>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProjectService> _logger;
    private const string ApiEndpoint = "api/projects";

    public ProjectService(HttpClient httpClient, ILogger<ProjectService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<List<Project>>> GetAllAsync()
    {
        try
        {
            var projects = await _httpClient.GetFromJsonAsync<List<Project>>(ApiEndpoint);
            return new ApiResponse<List<Project>> { Success = true, Data = projects ?? new List<Project>(), StatusCode = 200 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching projects");
            return new ApiResponse<List<Project>> { Success = false, ErrorMessage = "Failed to fetch projects.", StatusCode = 500 };
        }
    }

    public async Task<ApiResponse<Project>> GetByIdAsync(int id)
    {
        try
        {
            var project = await _httpClient.GetFromJsonAsync<Project>($"{ApiEndpoint}/{id}");
            return project == null 
                ? new ApiResponse<Project> { Success = false, ErrorMessage = "Project not found.", StatusCode = 404 }
                : new ApiResponse<Project> { Success = true, Data = project, StatusCode = 200 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project");
            return new ApiResponse<Project> { Success = false, ErrorMessage = "Failed to fetch project.", StatusCode = 500 };
        }
    }

    public async Task<ApiResponse<Project>> CreateAsync(Project item)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, item);
            if (response.IsSuccessStatusCode)
            {
                var createdProject = await response.Content.ReadFromJsonAsync<Project>();
                return new ApiResponse<Project> { Success = true, Data = createdProject, StatusCode = (int)response.StatusCode };
            }
            return new ApiResponse<Project> { Success = false, ErrorMessage = "Failed to create project.", StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            return new ApiResponse<Project> { Success = false, ErrorMessage = "Error creating project.", StatusCode = 500 };
        }
    }

    public async Task<ApiResponse<Project>> UpdateAsync(int id, Project item)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiEndpoint}/{id}", item);
            return response.IsSuccessStatusCode
                ? new ApiResponse<Project> { Success = true, Data = item, StatusCode = (int)response.StatusCode }
                : new ApiResponse<Project> { Success = false, ErrorMessage = "Failed to update project.", StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project");
            return new ApiResponse<Project> { Success = false, ErrorMessage = "Error updating project.", StatusCode = 500 };
        }
    }

    public async Task<ApiResponse> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiEndpoint}/{id}");
            return response.IsSuccessStatusCode
                ? new ApiResponse { Success = true, StatusCode = (int)response.StatusCode }
                : new ApiResponse { Success = false, ErrorMessage = "Failed to delete project.", StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project");
            return new ApiResponse { Success = false, ErrorMessage = "Error deleting project.", StatusCode = 500 };
        }
    }
}
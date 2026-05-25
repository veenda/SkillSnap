using SkillSnap.Shared.Models;
using System.Net.Http.Json;

namespace SkillSnap.Client.Services;

/// <summary>
/// Service for managing Projects via API with comprehensive error handling and logging
/// Implements IApiService<Project> for consistency with other data services
/// </summary>
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

    /// <summary>
    /// Retrieves all projects from the API with in-memory caching
    /// </summary>
    /// <returns>API response containing list of projects or error details</returns>
    public async Task<ApiResponse<List<Project>>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all projects from {Endpoint}", ApiEndpoint);
            
            var projects = await _httpClient.GetFromJsonAsync<List<Project>>(ApiEndpoint);
            
            return new ApiResponse<List<Project>>
            {
                Success = true,
                Data = projects ?? new List<Project>(),
                StatusCode = 200
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching projects");
            return new ApiResponse<List<Project>>
            {
                Success = false,
                ErrorMessage = "Failed to fetch projects. Please try again.",
                StatusCode = 500
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching projects");
            return new ApiResponse<List<Project>>
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred.",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Retrieves a single project by ID
    /// </summary>
    /// <param name="id">The project ID to fetch</param>
    /// <returns>API response containing the project or error details</returns>
    public async Task<ApiResponse<Project>> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching project with ID {ProjectId}", id);
            
            var project = await _httpClient.GetFromJsonAsync<Project>($"{ApiEndpoint}/{id}");
            
            if (project == null)
            {
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "Project not found.",
                    StatusCode = 404
                };
            }

            return new ApiResponse<Project>
            {
                Success = true,
                Data = project,
                StatusCode = 200
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new ApiResponse<Project>
            {
                Success = false,
                ErrorMessage = "Project not found.",
                StatusCode = 404
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project with ID {ProjectId}", id);
            return new ApiResponse<Project>
            {
                Success = false,
                ErrorMessage = "Failed to fetch project.",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Creates a new project (Admin only)
    /// </summary>
    /// <param name="item">The project data to create</param>
    /// <returns>API response containing the created project or error details</returns>
    public async Task<ApiResponse<Project>> CreateAsync(Project item)
    {
        try
        {
            if (item == null)
            {
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "Project data is required.",
                    StatusCode = 400
                };
            }

            _logger.LogInformation("Creating new project: {ProjectTitle}", item.Title);
            
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, item);
            
            if (response.IsSuccessStatusCode)
            {
                var createdProject = await response.Content.ReadAsAsync<Project>();
                _logger.LogInformation("Project created successfully with ID {ProjectId}", createdProject?.Id);
                
                return new ApiResponse<Project>
                {
                    Success = true,
                    Data = createdProject,
                    StatusCode = (int)response.StatusCode
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized project creation attempt");
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "You are not authenticated. Please log in.",
                    StatusCode = 401
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Forbidden project creation attempt - insufficient permissions");
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "You don't have permission to create projects. Admin access required.",
                    StatusCode = 403
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create project: HTTP {StatusCode} - {ErrorContent}", 
                    response.StatusCode, errorContent);
                
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "Failed to create project.",
                    StatusCode = (int)response.StatusCode
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while creating project");
            return new ApiResponse<Project>
            {
                Success = false,
                ErrorMessage = "Network error. Please try again.",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Updates an existing project (Admin only)
    /// </summary>
    /// <param name="id">The project ID to update</param>
    /// <param name="item">The updated project data</param>
    /// <returns>API response indicating success or failure</returns>
    public async Task<ApiResponse<Project>> UpdateAsync(int id, Project item)
    {
        try
        {
            if (item == null || item.Id != id)
            {
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "Invalid project data or ID mismatch.",
                    StatusCode = 400
                };
            }

            _logger.LogInformation("Updating project with ID {ProjectId}", id);
            
            var response = await _httpClient.PutAsJsonAsync($"{ApiEndpoint}/{id}", item);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Project with ID {ProjectId} updated successfully", id);
                
                return new ApiResponse<Project>
                {
                    Success = true,
                    Data = item,
                    StatusCode = (int)response.StatusCode
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Forbidden project update attempt for ID {ProjectId}", id);
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "You don't have permission to update projects.",
                    StatusCode = 403
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "Project not found.",
                    StatusCode = 404
                };
            }
            else
            {
                return new ApiResponse<Project>
                {
                    Success = false,
                    ErrorMessage = "Failed to update project.",
                    StatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project with ID {ProjectId}", id);
            return new ApiResponse<Project>
            {
                Success = false,
                ErrorMessage = "Failed to update project.",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Deletes a project (Admin only)
    /// </summary>
    /// <param name="id">The project ID to delete</param>
    /// <returns>API response indicating success or failure</returns>
    public async Task<ApiResponse> DeleteAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting project with ID {ProjectId}", id);
            
            var response = await _httpClient.DeleteAsync($"{ApiEndpoint}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Project with ID {ProjectId} deleted successfully", id);
                
                return new ApiResponse
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Forbidden project deletion attempt for ID {ProjectId}", id);
                return new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "You don't have permission to delete projects.",
                    StatusCode = 403
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "Project not found.",
                    StatusCode = 404
                };
            }
            else
            {
                return new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to delete project.",
                    StatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project with ID {ProjectId}", id);
            return new ApiResponse
            {
                Success = false,
                ErrorMessage = "Failed to delete project.",
                StatusCode = 500
            };
        }
    }
}

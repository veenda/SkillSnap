using SkillSnap.Shared.Models;
using System.Net.Http.Json;

namespace SkillSnap.Client.Services;

/// <summary>
/// API response wrapper for consistent error handling across services
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
}

/// <summary>
/// Base service for API operations with error handling and logging
/// </summary>
public interface IApiService<T> where T : class
{
    Task<ApiResponse<List<T>>> GetAllAsync();
    Task<ApiResponse<T>> GetByIdAsync(int id);
    Task<ApiResponse<T>> CreateAsync(T item);
    Task<ApiResponse<T>> UpdateAsync(int id, T item);
    Task<ApiResponse> DeleteAsync(int id);
}

/// <summary>
/// Generic service for managing Skills via API with proper error handling and logging
/// </summary>
public class SkillService : IApiService<Skill>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SkillService> _logger;
    private const string ApiEndpoint = "api/skills";

    public SkillService(HttpClient httpClient, ILogger<SkillService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all skills from the API with in-memory caching
    /// </summary>
    /// <returns>API response containing list of skills or error details</returns>
    public async Task<ApiResponse<List<Skill>>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all skills from {Endpoint}", ApiEndpoint);
            
            var skills = await _httpClient.GetFromJsonAsync<List<Skill>>(ApiEndpoint);
            
            return new ApiResponse<List<Skill>>
            {
                Success = true,
                Data = skills ?? new List<Skill>(),
                StatusCode = 200
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching skills");
            return new ApiResponse<List<Skill>>
            {
                Success = false,
                ErrorMessage = "Failed to fetch skills. Please try again.",
                StatusCode = 500
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching skills");
            return new ApiResponse<List<Skill>>
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred.",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Retrieves a single skill by ID
    /// </summary>
    public async Task<ApiResponse<Skill>> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching skill with ID {SkillId}", id);
            
            var skill = await _httpClient.GetFromJsonAsync<Skill>($"{ApiEndpoint}/{id}");
            
            if (skill == null)
            {
                return new ApiResponse<Skill>
                {
                    Success = false,
                    ErrorMessage = "Skill not found.",
                    StatusCode = 404
                };
            }

            return new ApiResponse<Skill>
            {
                Success = true,
                Data = skill,
                StatusCode = 200
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new ApiResponse<Skill>
            {
                Success = false,
                ErrorMessage = "Skill not found.",
                StatusCode = 404
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching skill with ID {SkillId}", id);
            return new ApiResponse<Skill>
            {
                Success = false,
                ErrorMessage = "Failed to fetch skill.",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Creates a new skill (Admin only)
    /// </summary>
    public async Task<ApiResponse<Skill>> CreateAsync(Skill item)
    {
        try
        {
            if (item == null)
            {
                return new ApiResponse<Skill>
                {
                    Success = false,
                    ErrorMessage = "Skill data is required.",
                    StatusCode = 400
                };
            }

            _logger.LogInformation("Creating new skill: {SkillName}", item.Name);
            
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, item);
            
            if (response.IsSuccessStatusCode)
            {
                var createdSkill = await response.Content.ReadAsAsync<Skill>();
                _logger.LogInformation("Skill created successfully with ID {SkillId}", createdSkill?.Id);
                
                return new ApiResponse<Skill>
                {
                    Success = true,
                    Data = createdSkill,
                    StatusCode = (int)response.StatusCode
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResponse<Skill>
                {
                    Success = false,
                    ErrorMessage = "You are not authenticated. Please log in.",
                    StatusCode = 401
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new ApiResponse<Skill>
                {
                    Success = false,
                    ErrorMessage = "You don't have permission to create skills. Admin access required.",
                    StatusCode = 403
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create skill: {ErrorContent}", errorContent);
                
                return new ApiResponse<Skill>
                {
                    Success = false,
                    ErrorMessage = "Failed to create skill.",
                    StatusCode = (int)response.StatusCode
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while creating skill");
            return new ApiResponse<Skill>
            {
                Success = false,
                ErrorMessage = "Network error. Please try again.",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Updates an existing skill (Admin only)
    /// </summary>
    public async Task<ApiResponse<Skill>> UpdateAsync(int id, Skill item)
    {
        try
        {
            if (item == null || item.Id != id)
            {
                return new ApiResponse<Skill>
                {
                    Success = false,
                    ErrorMessage = "Invalid skill data.",
                    StatusCode = 400
                };
            }

            _logger.LogInformation("Updating skill with ID {SkillId}", id);
            
            var response = await _httpClient.PutAsJsonAsync($"{ApiEndpoint}/{id}", item);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Skill with ID {SkillId} updated successfully", id);
                
                return new ApiResponse<Skill>
                {
                    Success = true,
                    Data = item,
                    StatusCode = (int)response.StatusCode
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new ApiResponse<Skill>
                {
                    Success = false,
                    ErrorMessage = "You don't have permission to update skills.",
                    StatusCode = 403
                };
            }
            else
            {
                return new ApiResponse<Skill>
                {
                    Success = false,
                    ErrorMessage = "Failed to update skill.",
                    StatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill with ID {SkillId}", id);
            return new ApiResponse<Skill>
            {
                Success = false,
                ErrorMessage = "Failed to update skill.",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Deletes a skill (Admin only)
    /// </summary>
    public async Task<ApiResponse> DeleteAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting skill with ID {SkillId}", id);
            
            var response = await _httpClient.DeleteAsync($"{ApiEndpoint}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Skill with ID {SkillId} deleted successfully", id);
                
                return new ApiResponse
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "You don't have permission to delete skills.",
                    StatusCode = 403
                };
            }
            else
            {
                return new ApiResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to delete skill.",
                    StatusCode = (int)response.StatusCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill with ID {SkillId}", id);
            return new ApiResponse
            {
                Success = false,
                ErrorMessage = "Failed to delete skill.",
                StatusCode = 500
            };
        }
    }
}
using SkillSnap.Shared.Models;
using System.Net.Http.Json;

namespace SkillSnap.Client.Services;

// 1. THIS WAS MISSING: The non-generic response for Deletes
public class ApiResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
}

// 2. The generic response for returning Data
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
}

// 3. The Shared Interface
public interface IApiService<T> where T : class
{
    Task<ApiResponse<List<T>>> GetAllAsync();
    Task<ApiResponse<T>> GetByIdAsync(int id);
    Task<ApiResponse<T>> CreateAsync(T item);
    Task<ApiResponse<T>> UpdateAsync(int id, T item);
    Task<ApiResponse> DeleteAsync(int id);
}

// 4. The actual Skill Service
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

    public async Task<ApiResponse<List<Skill>>> GetAllAsync()
    {
        try
        {
            var skills = await _httpClient.GetFromJsonAsync<List<Skill>>(ApiEndpoint);
            return new ApiResponse<List<Skill>> { Success = true, Data = skills ?? new List<Skill>(), StatusCode = 200 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching skills");
            return new ApiResponse<List<Skill>> { Success = false, ErrorMessage = "Failed to fetch skills.", StatusCode = 500 };
        }
    }

    public async Task<ApiResponse<Skill>> GetByIdAsync(int id)
    {
        try
        {
            var skill = await _httpClient.GetFromJsonAsync<Skill>($"{ApiEndpoint}/{id}");
            return skill == null 
                ? new ApiResponse<Skill> { Success = false, ErrorMessage = "Skill not found.", StatusCode = 404 }
                : new ApiResponse<Skill> { Success = true, Data = skill, StatusCode = 200 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching skill");
            return new ApiResponse<Skill> { Success = false, ErrorMessage = "Failed to fetch skill.", StatusCode = 500 };
        }
    }

    public async Task<ApiResponse<Skill>> CreateAsync(Skill item)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, item);
            if (response.IsSuccessStatusCode)
            {
                var createdSkill = await response.Content.ReadFromJsonAsync<Skill>();
                return new ApiResponse<Skill> { Success = true, Data = createdSkill, StatusCode = (int)response.StatusCode };
            }
            return new ApiResponse<Skill> { Success = false, ErrorMessage = "Failed to create skill.", StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating skill");
            return new ApiResponse<Skill> { Success = false, ErrorMessage = "Error creating skill.", StatusCode = 500 };
        }
    }

    public async Task<ApiResponse<Skill>> UpdateAsync(int id, Skill item)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiEndpoint}/{id}", item);
            return response.IsSuccessStatusCode
                ? new ApiResponse<Skill> { Success = true, Data = item, StatusCode = (int)response.StatusCode }
                : new ApiResponse<Skill> { Success = false, ErrorMessage = "Failed to update skill.", StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill");
            return new ApiResponse<Skill> { Success = false, ErrorMessage = "Error updating skill.", StatusCode = 500 };
        }
    }

    public async Task<ApiResponse> DeleteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiEndpoint}/{id}");
            return response.IsSuccessStatusCode
                ? new ApiResponse { Success = true, StatusCode = (int)response.StatusCode }
                : new ApiResponse { Success = false, ErrorMessage = "Failed to delete skill.", StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill");
            return new ApiResponse { Success = false, ErrorMessage = "Error deleting skill.", StatusCode = 500 };
        }
    }
}
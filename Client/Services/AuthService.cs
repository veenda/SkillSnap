using SkillSnap.Shared.Models;
using System.Net.Http.Json;
using Microsoft.JSInterop; // local storage access

namespace SkillSnap.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "jwt_token";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    // Register
    public async Task<AuthResponse?> RegisterAsync(RegisterModel model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", model);
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(jsonContent, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result?.Token != null)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
                    SetAuthorizationHeader(result.Token);
                }
                return result;
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Register error: {ex.Message}");
            return null;
        }
    }

    // Login
    public async Task<AuthResponse?> LoginAsync(LoginModel model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(jsonContent,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result?.Token != null)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
                    SetAuthorizationHeader(result.Token);
                }
                return result;
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Login error: {ex.Message}");
            return null;
        }
    }

    // Logout
    public async Task Logout()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        SetAuthorizationHeader(string.Empty);
    }

    // Intialize auth state on app startup
    public async Task InitializeAuthStateAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        if (!string.IsNullOrEmpty(token))
        {
            SetAuthorizationHeader(token);
        }
    }

    // IsAuthenticated
    public bool IsAuthenticated()
    {
        return _httpClient.DefaultRequestHeaders.Authorization != null;
    }

    // Helper to set the Authorization header for HttpClient
    private void SetAuthorizationHeader(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}

public class AuthResponse
{
    public string? Token { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
}

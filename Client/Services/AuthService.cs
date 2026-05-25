using SkillSnap.Shared.Models;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;

namespace SkillSnap.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private const string TokenKey = "jwt_token";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
    }

    // RegisterAsync
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
                    ((AuthStateProvider)_authStateProvider).MarkUserAsAuthenticated(model.Email);
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

    // LoginAsync
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
                    ((AuthStateProvider)_authStateProvider).MarkUserAsAuthenticated(model.Email);
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
        ((AuthStateProvider)_authStateProvider).MarkUserAsLoggedOut();
    }

    // Intialize auth state on app startup
    public async Task InitializeAuthStateAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "jwt_token");
        if (!string.IsNullOrEmpty(token))
        {
            SetAuthorizationHeader(token);
        }
    }

    // IsAuthenticated
    public bool IsAuthenticated() => _httpClient.DefaultRequestHeaders.Authorization != null;

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
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
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(content, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (result?.Token != null)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
                    SetAuthorizationHeader(result.Token);
                    ((AuthStateProvider)_authStateProvider).MarkUserAsAuthenticated(model.Email);
                }
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"Registration failed: {errorContent}");
                return new AuthResponse { Message = "Registration failed. Please try again." };
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Register error: {ex.Message}");
            return new AuthResponse { Message = "An error occurred during registration. Please try again." };
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
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result?.Token != null)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
                    SetAuthorizationHeader(result.Token);
                    ((AuthStateProvider)_authStateProvider).MarkUserAsAuthenticated(model.Email);
                }
                return result;
            }
            return new AuthResponse { Message = "Invalid email or password." };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Login error: {ex.Message}");
            return new AuthResponse { Message = "An error occurred during login. Please try again." };
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
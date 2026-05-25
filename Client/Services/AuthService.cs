using SkillSnap.Shared.Models;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;

namespace SkillSnap.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IUserStateService _userStateService;
    private const string TokenKey = "jwt_token";

    public AuthService(
        HttpClient httpClient, 
        IJSRuntime jsRuntime, 
        AuthenticationStateProvider authStateProvider,
        IUserStateService userStateService)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
        _userStateService = userStateService;
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
                    ExtractAndSetUserInfo(result.Token);
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
                    ExtractAndSetUserInfo(result.Token);
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
        _userStateService.ClearSession();
        ((AuthStateProvider)_authStateProvider).MarkUserAsLoggedOut();
    }

    // Intialize auth state on app startup
    public async Task InitializeAuthStateAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "jwt_token");
        if (!string.IsNullOrEmpty(token))
        {
            SetAuthorizationHeader(token);
            ExtractAndSetUserInfo(token);
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

    // Helper to extract user info from JWT token
    private void ExtractAndSetUserInfo(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken != null)
            {
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "User";

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(email))
                {
                    _userStateService.SetUser(userId, email, role, jwtToken.ValidTo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error extracting user info from token: {ex.Message}");
        }
    }
}

public class AuthResponse
{
    public string? Token { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
}
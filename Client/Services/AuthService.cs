using SkillSnap.Shared.Models;
using System.Net.Http.Json;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

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

    public void Logout()
    {
        SetAuthorizationHeader(string.Empty);
    }

    public bool IsAuthenticated()
    {
        return _httpClient.DefaultRequestHeaders.Authorization != null;
    }

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

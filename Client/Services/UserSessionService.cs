namespace SkillSnap.Client.Services;

public class UserSessionService
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }

    public event Action? OnChange;

    public void SetUser(string userId, string email, string role)
    {
        UserId = userId;
        Email = email;
        Role = role;
        NotifyStateChanged();
    }

    public void ClearSession()
    {
        UserId = null;
        Email = null;
        Role = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
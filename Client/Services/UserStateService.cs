using System;
using System.Collections.Generic;

namespace SkillSnap.Client.Services;

/// <summary>
/// Service to manage and persist logged-in user information with caching
/// </summary>
public interface IUserStateService
{
    event Action? OnStateChanged;

    string? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    DateTime? ExpirationTime { get; }

    void SetUser(string userId, string email, string role, DateTime? expiration = null);
    void ClearSession();
    bool IsExpired();
}

public class UserStateService : IUserStateService
{
    public event Action? OnStateChanged;

    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public DateTime? ExpirationTime { get; private set; }

    public void SetUser(string userId, string email, string role, DateTime? expiration = null)
    {
        UserId = userId;
        Email = email;
        Role = role;
        ExpirationTime = expiration ?? DateTime.UtcNow.AddHours(1);
        NotifyStateChanged();
    }

    public void ClearSession()
    {
        UserId = null;
        Email = null;
        Role = null;
        ExpirationTime = null;
        NotifyStateChanged();
    }

    public bool IsExpired()
    {
        return ExpirationTime.HasValue && DateTime.UtcNow >= ExpirationTime.Value;
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}

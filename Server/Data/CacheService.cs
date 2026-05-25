using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace SkillSnap.Server.Services;

/// <summary>
/// Generic caching service with expiration handling and fallback logic
/// </summary>
public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        Func<Task<T?>>? fallback = null);

    void Remove(string key);
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private const int DefaultExpirationMinutes = 5;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        Func<Task<T?>>? fallback = null)
    {
        try
        {
            if (_cache.TryGetValue(key, out T? cachedValue))
            {
                _logger.LogInformation("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogInformation("Cache miss for key: {Key}", key);
            var value = await factory();

            expiration ??= TimeSpan.FromMinutes(DefaultExpirationMinutes);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(expiration.Value);

            _cache.Set(key, value, cacheOptions);
            _logger.LogInformation("Cached value for key: {Key}", key);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing cache factory for key: {Key}", key);

            if (fallback != null)
            {
                try
                {
                    var fallbackValue = await fallback();
                    _logger.LogInformation("Fallback succeeded for key: {Key}", key);
                    return fallbackValue;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback also failed for key: {Key}", key);
                }
            }

            throw;
        }
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogInformation("Cache entry removed for key: {Key}", key);
    }
}

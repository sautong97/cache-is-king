using CacheIsKing.Core.Interfaces;

namespace CacheIsKing.Tests.Mocks;

/// <summary>
/// Simple mock implementation of IHybridCacheService for testing cache behavior
/// </summary>
public class MockHybridCacheService : IHybridCacheService
{
    private readonly Dictionary<string, (object Value, DateTime Expiry)> _cache = new();
    private readonly Dictionary<string, int> _accessCount = new();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        await Task.Yield(); // Make it properly async
        
        IncrementAccessCount(key);
        
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.Expiry > DateTime.UtcNow)
            {
                return cached.Value as T;
            }
            // Expired, remove from cache
            _cache.Remove(key);
        }
        
        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        await Task.Yield(); // Make it properly async
        
        var expiryTime = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(1));
        _cache[key] = (value!, expiryTime);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make it properly async
        
        _cache.Remove(key);
        _accessCount.Remove(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make it properly async
        
        return _cache.ContainsKey(key) && _cache[key].Expiry > DateTime.UtcNow;
    }

    public string GenerateCacheKey(string prefix, params object[] parameters)
    {
        var paramString = string.Join("|", parameters.Select(p => p?.ToString() ?? "null"));
        return $"{prefix}:{paramString.GetHashCode():X}";
    }

    private void IncrementAccessCount(string key)
    {
        _accessCount[key] = _accessCount.GetValueOrDefault(key, 0) + 1;
    }

    /// <summary>
    /// Get the number of times a cache key was accessed
    /// </summary>
    public int GetAccessCount(string key) => _accessCount.GetValueOrDefault(key, 0);

    /// <summary>
    /// Check if a key exists in the mock cache
    /// </summary>
    public bool HasKey(string key) => _cache.ContainsKey(key) && _cache[key].Expiry > DateTime.UtcNow;

    /// <summary>
    /// Get all cache keys
    /// </summary>
    public IEnumerable<string> GetAllKeys() => _cache.Keys;

    /// <summary>
    /// Clear all cache data
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _accessCount.Clear();
    }

    /// <summary>
    /// Simulate cache expiry for a specific key
    /// </summary>
    public void ExpireKey(string key)
    {
        if (_cache.ContainsKey(key))
        {
            _cache[key] = (_cache[key].Value, DateTime.UtcNow.AddSeconds(-1));
        }
    }
}

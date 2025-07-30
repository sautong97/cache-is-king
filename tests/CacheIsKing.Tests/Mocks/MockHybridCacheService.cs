using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace CacheIsKing.Tests.Mocks;

/// <summary>
/// Mock implementation of IHybridCacheService for testing cache behavior
/// </summary>
public class MockHybridCacheService : Mock<IHybridCacheService>
{
    private readonly Dictionary<string, (object Value, DateTime Expiry)> _cache = new();
    private readonly Dictionary<string, int> _accessCount = new();

    public MockHybridCacheService()
    {
        Setup(x => x.GetAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((key, _) =>
            {
                IncrementAccessCount(key);
                
                if (_cache.TryGetValue(key, out var cached))
                {
                    if (cached.Expiry > DateTime.UtcNow)
                    {
                        return Task.FromResult((object?)cached.Value);
                    }
                    // Expired, remove from cache
                    _cache.Remove(key);
                }
                
                return Task.FromResult((object?)null);
            });

        Setup(x => x.SetAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns<object, string, TimeSpan?, CancellationToken>((value, key, expiry, _) =>
            {
                var expiryTime = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(1));
                _cache[key] = (value, expiryTime);
                return Task.CompletedTask;
            });

        Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((key, _) =>
            {
                _cache.Remove(key);
                _accessCount.Remove(key);
                return Task.CompletedTask;
            });

        Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((key, _) =>
            {
                var exists = _cache.ContainsKey(key) && _cache[key].Expiry > DateTime.UtcNow;
                return Task.FromResult(exists);
            });

        Setup(x => x.GenerateCacheKey(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((prefix, parameters) =>
            {
                var paramString = string.Join("|", parameters.Select(p => p?.ToString() ?? "null"));
                return $"{prefix}:{paramString.GetHashCode():X}";
            });
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

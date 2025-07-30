using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CacheIsKing.Caching;

/// <summary>
/// Hybrid cache implementation using both in-memory and distributed (Redis) cache
/// </summary>
public class HybridCacheService : IHybridCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<HybridCacheService> _logger;
    
    private static readonly TimeSpan DefaultMemoryCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultDistributedCacheTtl = TimeSpan.FromHours(1);

    public HybridCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<HybridCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // First, try memory cache
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                _logger.LogDebug("Cache hit (memory): {Key}", key);
                return cachedValue;
            }

            // Then try distributed cache
            var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                _logger.LogDebug("Cache hit (distributed): {Key}", key);
                var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue);
                
                // Store in memory cache for faster subsequent access
                _memoryCache.Set(key, deserializedValue, DefaultMemoryCacheTtl);
                
                return deserializedValue;
            }

            _logger.LogDebug("Cache miss: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var effectiveExpiry = expiry ?? DefaultDistributedCacheTtl;
            var memoryExpiry = TimeSpan.FromTicks(Math.Min(effectiveExpiry.Ticks, DefaultMemoryCacheTtl.Ticks));

            // Set in memory cache
            _memoryCache.Set(key, value, memoryExpiry);

            // Set in distributed cache
            var serializedValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = effectiveExpiry
            };

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            
            _logger.LogDebug("Cache set: {Key} (expires in {Expiry})", key, effectiveExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            await _distributedCache.RemoveAsync(key, cancellationToken);
            
            _logger.LogDebug("Cache removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check memory cache first
            if (_memoryCache.TryGetValue(key, out _))
            {
                return true;
            }

            // Check distributed cache
            var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(distributedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence: {Key}", key);
            return false;
        }
    }

    public string GenerateCacheKey(string prefix, params object[] parameters)
    {
        return CacheKeyGenerator.GenerateKey(prefix, parameters);
    }
}

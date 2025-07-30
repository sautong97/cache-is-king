namespace CacheIsKing.Core.Interfaces;

/// <summary>
/// Hybrid caching interface supporting both in-memory and distributed cache
/// </summary>
public interface IHybridCacheService
{
    /// <summary>
    /// Get value from cache (checks memory cache first, then distributed cache)
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Set value in both memory and distributed cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Remove value from both caches
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if key exists in either cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate cache key from input parameters
    /// </summary>
    string GenerateCacheKey(string prefix, params object[] parameters);
}

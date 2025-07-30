using CacheIsKing.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CacheIsKing.Caching.Extensions;

/// <summary>
/// Extension methods for configuring caching services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add hybrid caching services (memory + distributed)
    /// </summary>
    public static IServiceCollection AddHybridCaching(
        this IServiceCollection services, 
        string? redisConnectionString = null)
    {
        // Add memory cache
        services.AddMemoryCache();
        
        // Add distributed cache (Redis if connection string provided, otherwise in-memory)
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "CacheIsKing";
            });
        }
        else
        {
            // Fallback to distributed memory cache for development
            services.AddDistributedMemoryCache();
        }
        
        // Register hybrid cache service
        services.AddScoped<IHybridCacheService, HybridCacheService>();
        
        return services;
    }
}

using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using CacheIsKing.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CacheIsKing.Aggregation;

/// <summary>
/// Aggregation service that orchestrates multiple location providers with caching
/// </summary>
public class LocationService : ILocationService
{
    private readonly IEnumerable<ILocationProviderService> _providers;
    private readonly IHybridCacheService _cacheService;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        IEnumerable<ILocationProviderService> providers,
        IHybridCacheService cacheService,
        ILogger<LocationService> logger)
    {
        _providers = providers.OrderByDescending(p => p.ProviderName == "TomTom" ? 1 : 0); // Prioritize TomTom
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<GeocodeResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyGenerator.ForGeocode(address);
        
        // Try cache first
        var cachedResult = await _cacheService.GetAsync<GeocodeResult>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogDebug("Geocode cache hit for address: {Address}", address);
            return cachedResult;
        }

        // Try each provider in priority order
        foreach (var provider in _providers)
        {
            try
            {
                _logger.LogDebug("Attempting geocode with provider: {Provider}", provider.ProviderName);
                var result = await provider.GeocodeAsync(address, cancellationToken);
                
                if (result.Coordinates != null)
                {
                    // Cache the result if provider allows it
                    if (provider.AllowsCaching)
                    {
                        await _cacheService.SetAsync(cacheKey, result, provider.CacheTtl, cancellationToken);
                        _logger.LogDebug("Cached geocode result from {Provider}", provider.ProviderName);
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed for geocoding: {Address}", provider.ProviderName, address);
                continue;
            }
        }

        // If all providers fail, return empty result
        _logger.LogError("All providers failed for geocoding: {Address}", address);
        return new GeocodeResult { Address = address, ProviderName = "None" };
    }

    public async Task<RouteResult> GetRouteAsync(Coordinates from, Coordinates to, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyGenerator.ForRoute(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        
        // Try cache first
        var cachedResult = await _cacheService.GetAsync<RouteResult>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogDebug("Route cache hit for {From} to {To}", from, to);
            return cachedResult;
        }

        // Try each provider in priority order
        foreach (var provider in _providers)
        {
            try
            {
                _logger.LogDebug("Attempting route calculation with provider: {Provider}", provider.ProviderName);
                var result = await provider.GetRouteAsync(from, to, cancellationToken);
                
                if (result.DistanceMeters > 0)
                {
                    // Cache the result if provider allows it
                    if (provider.AllowsCaching)
                    {
                        await _cacheService.SetAsync(cacheKey, result, provider.CacheTtl, cancellationToken);
                        _logger.LogDebug("Cached route result from {Provider}", provider.ProviderName);
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed for route calculation: {From} to {To}", provider.ProviderName, from, to);
                continue;
            }
        }

        // If all providers fail, return empty result
        _logger.LogError("All providers failed for route calculation: {From} to {To}", from, to);
        return new RouteResult { Origin = from, Destination = to, ProviderName = "None" };
    }

    public async Task<GeocodeResult> ReverseGeocodeAsync(Coordinates coordinates, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyGenerator.ForReverseGeocode(coordinates.Latitude, coordinates.Longitude);
        
        // Try cache first
        var cachedResult = await _cacheService.GetAsync<GeocodeResult>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogDebug("Reverse geocode cache hit for coordinates: {Coordinates}", coordinates);
            return cachedResult;
        }

        // Try each provider in priority order
        foreach (var provider in _providers)
        {
            try
            {
                _logger.LogDebug("Attempting reverse geocode with provider: {Provider}", provider.ProviderName);
                var result = await provider.ReverseGeocodeAsync(coordinates, cancellationToken);
                
                if (!string.IsNullOrEmpty(result.FormattedAddress))
                {
                    // Cache the result if provider allows it
                    if (provider.AllowsCaching)
                    {
                        await _cacheService.SetAsync(cacheKey, result, provider.CacheTtl, cancellationToken);
                        _logger.LogDebug("Cached reverse geocode result from {Provider}", provider.ProviderName);
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed for reverse geocoding: {Coordinates}", provider.ProviderName, coordinates);
                continue;
            }
        }

        // If all providers fail, return empty result
        _logger.LogError("All providers failed for reverse geocoding: {Coordinates}", coordinates);
        return new GeocodeResult { Coordinates = coordinates, ProviderName = "None" };
    }

    public async Task<Dictionary<string, bool>> GetProvidersHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthResults = new Dictionary<string, bool>();
        
        var healthTasks = _providers.Select(async provider =>
        {
            try
            {
                var isHealthy = await provider.IsHealthyAsync(cancellationToken);
                return new { Provider = provider.ProviderName, IsHealthy = isHealthy };
            }
            catch
            {
                return new { Provider = provider.ProviderName, IsHealthy = false };
            }
        });

        var results = await Task.WhenAll(healthTasks);
        
        foreach (var result in results)
        {
            healthResults[result.Provider] = result.IsHealthy;
        }

        return healthResults;
    }
}

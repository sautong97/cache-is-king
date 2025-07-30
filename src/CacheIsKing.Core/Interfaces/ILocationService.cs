using CacheIsKing.Core.Models;

namespace CacheIsKing.Core.Interfaces;

/// <summary>
/// Service for aggregating and orchestrating multiple location providers
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Geocode address using the best available provider
    /// </summary>
    Task<GeocodeResult> GeocodeAsync(string address, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate route using the best available provider
    /// </summary>
    Task<RouteResult> GetRouteAsync(Coordinates from, Coordinates to, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reverse geocode coordinates using the best available provider
    /// </summary>
    Task<GeocodeResult> ReverseGeocodeAsync(Coordinates coordinates, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get health status of all providers
    /// </summary>
    Task<Dictionary<string, bool>> GetProvidersHealthAsync(CancellationToken cancellationToken = default);
}

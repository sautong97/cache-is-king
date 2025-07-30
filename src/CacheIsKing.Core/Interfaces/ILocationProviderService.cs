using CacheIsKing.Core.Models;

namespace CacheIsKing.Core.Interfaces;

/// <summary>
/// Interface for location service providers (TomTom, HERE, Google Maps, etc.)
/// </summary>
public interface ILocationProviderService
{
    /// <summary>
    /// Convert address to coordinates
    /// </summary>
    Task<GeocodeResult> GeocodeAsync(string address, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate route between two points
    /// </summary>
    Task<RouteResult> GetRouteAsync(Coordinates from, Coordinates to, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Convert coordinates to address
    /// </summary>
    Task<GeocodeResult> ReverseGeocodeAsync(Coordinates coordinates, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Whether this provider allows caching of responses
    /// </summary>
    bool AllowsCaching { get; }
    
    /// <summary>
    /// Time-to-live for cache entries from this provider
    /// </summary>
    TimeSpan? CacheTtl { get; }
    
    /// <summary>
    /// Name of the provider
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Whether the provider is currently available
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

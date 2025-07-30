using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace CacheIsKing.Gateway.Controllers;

/// <summary>
/// API Controller for location services (geocoding, routing, reverse geocoding)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(ILocationService locationService, ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Geocode an address to coordinates
    /// </summary>
    /// <param name="address">The address to geocode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Geocoding result with coordinates</returns>
    [HttpGet("geocode")]
    public async Task<ActionResult<GeocodeResult>> GeocodeAsync(
        [FromQuery] string address, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return BadRequest("Address is required");
        }

        try
        {
            _logger.LogInformation("Geocoding request for address: {Address}", address);
            var result = await _locationService.GeocodeAsync(address, cancellationToken);
            
            if (result.Coordinates == null)
            {
                return NotFound($"Could not geocode address: {address}");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return StatusCode(500, "Internal server error occurred while geocoding");
        }
    }

    /// <summary>
    /// Reverse geocode coordinates to address
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reverse geocoding result with address</returns>
    [HttpGet("reverse-geocode")]
    public async Task<ActionResult<GeocodeResult>> ReverseGeocodeAsync(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        CancellationToken cancellationToken = default)
    {
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            return BadRequest("Invalid coordinates. Latitude must be between -90 and 90, longitude between -180 and 180");
        }

        try
        {
            var coordinates = new Coordinates(latitude, longitude);
            _logger.LogInformation("Reverse geocoding request for coordinates: {Coordinates}", coordinates);
            
            var result = await _locationService.ReverseGeocodeAsync(coordinates, cancellationToken);
            
            if (string.IsNullOrEmpty(result.FormattedAddress))
            {
                return NotFound($"Could not reverse geocode coordinates: {coordinates}");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding coordinates: {Latitude}, {Longitude}", latitude, longitude);
            return StatusCode(500, "Internal server error occurred while reverse geocoding");
        }
    }

    /// <summary>
    /// Calculate route between two points
    /// </summary>
    /// <param name="fromLat">Origin latitude</param>
    /// <param name="fromLon">Origin longitude</param>
    /// <param name="toLat">Destination latitude</param>
    /// <param name="toLon">Destination longitude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Route calculation result</returns>
    [HttpGet("route")]
    public async Task<ActionResult<RouteResult>> GetRouteAsync(
        [FromQuery] double fromLat,
        [FromQuery] double fromLon,
        [FromQuery] double toLat,
        [FromQuery] double toLon,
        CancellationToken cancellationToken = default)
    {
        if (fromLat < -90 || fromLat > 90 || fromLon < -180 || fromLon > 180 ||
            toLat < -90 || toLat > 90 || toLon < -180 || toLon > 180)
        {
            return BadRequest("Invalid coordinates. Latitude must be between -90 and 90, longitude between -180 and 180");
        }

        try
        {
            var from = new Coordinates(fromLat, fromLon);
            var to = new Coordinates(toLat, toLon);
            
            _logger.LogInformation("Route calculation request from {From} to {To}", from, to);
            
            var result = await _locationService.GetRouteAsync(from, to, cancellationToken);
            
            if (result.DistanceMeters <= 0)
            {
                return NotFound($"Could not calculate route from {from} to {to}");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating route from ({FromLat}, {FromLon}) to ({ToLat}, {ToLon})", fromLat, fromLon, toLat, toLon);
            return StatusCode(500, "Internal server error occurred while calculating route");
        }
    }

    /// <summary>
    /// Get health status of all location service providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status of all providers</returns>
    [HttpGet("health")]
    public async Task<ActionResult<Dictionary<string, bool>>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Health check request for all providers");
            var healthStatus = await _locationService.GetProvidersHealthAsync(cancellationToken);
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking providers health");
            return StatusCode(500, "Internal server error occurred while checking health");
        }
    }
}

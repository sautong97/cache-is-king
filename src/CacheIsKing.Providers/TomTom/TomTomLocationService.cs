using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CacheIsKing.Providers.TomTom;

/// <summary>
/// TomTom location service provider implementation
/// Note: TomTom's terms of service prohibit server-side caching
/// </summary>
public class TomTomLocationService : ILocationProviderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TomTomLocationService> _logger;
    private readonly string _apiKey;

    public string ProviderName => "TomTom";
    public bool AllowsCaching => false; // TomTom prohibits server-side caching
    public TimeSpan? CacheTtl => null;

    public TomTomLocationService(HttpClient httpClient, ILogger<TomTomLocationService> logger, string apiKey)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = apiKey;
        
        // Configure base URL
        _httpClient.BaseAddress = new Uri("https://api.tomtom.com/");
    }

    public async Task<GeocodeResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"search/2/geocode/{Uri.EscapeDataString(address)}.json?key={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tomtomResponse = JsonSerializer.Deserialize<TomTomGeocodeResponse>(content);

            if (tomtomResponse?.Results?.Any() == true)
            {
                var result = tomtomResponse.Results.First();
                return new GeocodeResult
                {
                    Address = address,
                    Coordinates = new Coordinates(result.Position.Lat, result.Position.Lon),
                    Confidence = result.Score,
                    FormattedAddress = result.Address.FreeformAddress,
                    CountryCode = result.Address.CountryCode,
                    PostalCode = result.Address.PostalCode,
                    City = result.Address.Municipality,
                    State = result.Address.CountrySubdivision,
                    ProviderName = ProviderName
                };
            }

            return new GeocodeResult { Address = address, ProviderName = ProviderName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address with TomTom: {Address}", address);
            throw;
        }
    }

    public async Task<RouteResult> GetRouteAsync(Coordinates from, Coordinates to, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"routing/1/calculateRoute/{from.Latitude},{from.Longitude}:{to.Latitude},{to.Longitude}/json?key={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tomtomResponse = JsonSerializer.Deserialize<TomTomRouteResponse>(content);

            if (tomtomResponse?.Routes?.Any() == true)
            {
                var route = tomtomResponse.Routes.First();
                return new RouteResult
                {
                    Origin = from,
                    Destination = to,
                    DistanceMeters = route.Summary.LengthInMeters,
                    Duration = TimeSpan.FromSeconds(route.Summary.TravelTimeInSeconds),
                    ProviderName = ProviderName
                };
            }

            return new RouteResult { Origin = from, Destination = to, ProviderName = ProviderName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating route with TomTom: {From} to {To}", from, to);
            throw;
        }
    }

    public async Task<GeocodeResult> ReverseGeocodeAsync(Coordinates coordinates, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"search/2/reverseGeocode/{coordinates.Latitude},{coordinates.Longitude}.json?key={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tomtomResponse = JsonSerializer.Deserialize<TomTomGeocodeResponse>(content);

            if (tomtomResponse?.Results?.Any() == true)
            {
                var result = tomtomResponse.Results.First();
                return new GeocodeResult
                {
                    Coordinates = coordinates,
                    FormattedAddress = result.Address.FreeformAddress,
                    CountryCode = result.Address.CountryCode,
                    PostalCode = result.Address.PostalCode,
                    City = result.Address.Municipality,
                    State = result.Address.CountrySubdivision,
                    ProviderName = ProviderName
                };
            }

            return new GeocodeResult { Coordinates = coordinates, ProviderName = ProviderName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding with TomTom: {Coordinates}", coordinates);
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check by geocoding a well-known address
            await GeocodeAsync("London", cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// TomTom API response models
internal class TomTomGeocodeResponse
{
    public TomTomResult[] Results { get; set; } = Array.Empty<TomTomResult>();
}

internal class TomTomResult
{
    public TomTomPosition Position { get; set; } = new();
    public TomTomAddress Address { get; set; } = new();
    public double Score { get; set; }
}

internal class TomTomPosition
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}

internal class TomTomAddress
{
    public string FreeformAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string CountrySubdivision { get; set; } = string.Empty;
}

internal class TomTomRouteResponse
{
    public TomTomRoute[] Routes { get; set; } = Array.Empty<TomTomRoute>();
}

internal class TomTomRoute
{
    public TomTomSummary Summary { get; set; } = new();
}

internal class TomTomSummary
{
    public int LengthInMeters { get; set; }
    public int TravelTimeInSeconds { get; set; }
}

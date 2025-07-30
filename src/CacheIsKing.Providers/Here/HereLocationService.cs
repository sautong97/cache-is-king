using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CacheIsKing.Providers.Here;

/// <summary>
/// HERE location service provider implementation
/// HERE allows caching with reasonable TTL
/// </summary>
public class HereLocationService : ILocationProviderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HereLocationService> _logger;
    private readonly string _apiKey;

    public string ProviderName => "HERE";
    public bool AllowsCaching => true; // HERE allows caching
    public TimeSpan? CacheTtl => TimeSpan.FromHours(6);

    public HereLocationService(HttpClient httpClient, ILogger<HereLocationService> logger, string apiKey)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = apiKey;
        
        // Configure base URL
        _httpClient.BaseAddress = new Uri("https://geocode.search.hereapi.com/");
    }

    public async Task<GeocodeResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"v1/geocode?q={Uri.EscapeDataString(address)}&apiKey={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var hereResponse = JsonSerializer.Deserialize<HereGeocodeResponse>(content);

            if (hereResponse?.Items?.Any() == true)
            {
                var result = hereResponse.Items.First();
                return new GeocodeResult
                {
                    Address = address,
                    Coordinates = new Coordinates(result.Position.Lat, result.Position.Lng),
                    Confidence = result.Scoring.QueryScore,
                    FormattedAddress = result.Title,
                    CountryCode = result.Address.CountryCode,
                    PostalCode = result.Address.PostalCode,
                    City = result.Address.City,
                    State = result.Address.State,
                    ProviderName = ProviderName
                };
            }

            return new GeocodeResult { Address = address, ProviderName = ProviderName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address with HERE: {Address}", address);
            throw;
        }
    }

    public async Task<RouteResult> GetRouteAsync(Coordinates from, Coordinates to, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://router.hereapi.com/v8/routes?transportMode=car&origin={from.Latitude},{from.Longitude}&destination={to.Latitude},{to.Longitude}&return=summary&apiKey={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var hereResponse = JsonSerializer.Deserialize<HereRouteResponse>(content);

            if (hereResponse?.Routes?.Any() == true)
            {
                var route = hereResponse.Routes.First();
                return new RouteResult
                {
                    Origin = from,
                    Destination = to,
                    DistanceMeters = route.Sections.First().Summary.Length,
                    Duration = TimeSpan.FromSeconds(route.Sections.First().Summary.Duration),
                    ProviderName = ProviderName
                };
            }

            return new RouteResult { Origin = from, Destination = to, ProviderName = ProviderName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating route with HERE: {From} to {To}", from, to);
            throw;
        }
    }

    public async Task<GeocodeResult> ReverseGeocodeAsync(Coordinates coordinates, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"v1/revgeocode?at={coordinates.Latitude},{coordinates.Longitude}&apiKey={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var hereResponse = JsonSerializer.Deserialize<HereGeocodeResponse>(content);

            if (hereResponse?.Items?.Any() == true)
            {
                var result = hereResponse.Items.First();
                return new GeocodeResult
                {
                    Coordinates = coordinates,
                    FormattedAddress = result.Title,
                    CountryCode = result.Address.CountryCode,
                    PostalCode = result.Address.PostalCode,
                    City = result.Address.City,
                    State = result.Address.State,
                    ProviderName = ProviderName
                };
            }

            return new GeocodeResult { Coordinates = coordinates, ProviderName = ProviderName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding with HERE: {Coordinates}", coordinates);
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

// HERE API response models
internal class HereGeocodeResponse
{
    public HereItem[] Items { get; set; } = Array.Empty<HereItem>();
}

internal class HereItem
{
    public string Title { get; set; } = string.Empty;
    public HerePosition Position { get; set; } = new();
    public HereAddress Address { get; set; } = new();
    public HereScoring Scoring { get; set; } = new();
}

internal class HerePosition
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

internal class HereAddress
{
    public string CountryCode { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

internal class HereScoring
{
    public double QueryScore { get; set; }
}

internal class HereRouteResponse
{
    public HereRoute[] Routes { get; set; } = Array.Empty<HereRoute>();
}

internal class HereRoute
{
    public HereSection[] Sections { get; set; } = Array.Empty<HereSection>();
}

internal class HereSection
{
    public HereSummary Summary { get; set; } = new();
}

internal class HereSummary
{
    public int Length { get; set; }
    public int Duration { get; set; }
}

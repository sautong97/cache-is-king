using Bogus;
using CacheIsKing.Core.Models;

namespace CacheIsKing.Tests.TestData;

/// <summary>
/// Factory for generating test data using Bogus library
/// </summary>
public static class TestDataFactory
{
    private static readonly Faker _faker = new();

    public static GeocodeResult CreateGeocodeResult(string? address = null, Coordinates? coordinates = null)
    {
        return new GeocodeResult
        {
            FormattedAddress = address ?? _faker.Address.FullAddress(),
            Address = address ?? _faker.Address.FullAddress(),
            Coordinates = coordinates ?? CreateCoordinates(),
            Confidence = _faker.Random.Double(0.5, 1.0),
            ProviderName = _faker.PickRandom("TomTom", "HERE", "GoogleMaps"),
            CountryCode = _faker.Address.CountryCode(),
            PostalCode = _faker.Address.ZipCode(),
            City = _faker.Address.City(),
            State = _faker.Address.State(),
            ResponseTime = DateTime.UtcNow.AddMilliseconds(-_faker.Random.Int(10, 500))
        };
    }

    public static RouteResult CreateRouteResult(Coordinates? from = null, Coordinates? to = null)
    {
        return new RouteResult
        {
            Origin = from ?? CreateCoordinates(),
            Destination = to ?? CreateCoordinates(),
            DistanceMeters = _faker.Random.Int(1000, 100000),
            Duration = TimeSpan.FromSeconds(_faker.Random.Int(300, 7200)),
            ProviderName = _faker.PickRandom("TomTom", "HERE", "GoogleMaps"),
            Instructions = _faker.Lorem.Sentences(3),
            RoutePoints = new List<Coordinates> { from ?? CreateCoordinates(), to ?? CreateCoordinates() },
            ResponseTime = DateTime.UtcNow.AddMilliseconds(-_faker.Random.Int(10, 500))
        };
    }

    public static Coordinates CreateCoordinates()
    {
        return new Coordinates(
            _faker.Address.Latitude(),
            _faker.Address.Longitude()
        );
    }

    public static string CreateAddress()
    {
        return _faker.Address.FullAddress();
    }

    public static List<GeocodeResult> CreateGeocodeResults(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateGeocodeResult())
            .ToList();
    }

    public static List<RouteResult> CreateRouteResults(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateRouteResult())
            .ToList();
    }

    public static Dictionary<string, bool> CreateHealthStatus()
    {
        return new Dictionary<string, bool>
        {
            { "TomTom", _faker.Random.Bool() },
            { "HERE", _faker.Random.Bool() },
            { "GoogleMaps", _faker.Random.Bool() }
        };
    }
}

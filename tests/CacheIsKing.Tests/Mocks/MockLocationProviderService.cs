using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using CacheIsKing.Tests.TestData;
using Moq;

namespace CacheIsKing.Tests.Mocks;

/// <summary>
/// Mock implementation of ILocationProviderService for testing without external API calls
/// </summary>
public class MockLocationProviderService : Mock<ILocationProviderService>
{
    private readonly string _providerName;
    private readonly bool _allowsCaching;
    private readonly TimeSpan? _cacheTtl;
    private readonly Dictionary<string, GeocodeResult> _geocodeResponses = new();
    private readonly Dictionary<string, RouteResult> _routeResponses = new();
    private readonly Queue<Exception> _exceptionsToThrow = new();
    private int _callCount = 0;
    private bool _isHealthy = true;

    public MockLocationProviderService(
        string providerName = "MockProvider",
        bool allowsCaching = true,
        TimeSpan? cacheTtl = null)
    {
        _providerName = providerName;
        _allowsCaching = allowsCaching;
        _cacheTtl = cacheTtl ?? TimeSpan.FromHours(1);

        SetupMockBehavior();
    }

    private void SetupMockBehavior()
    {
        SetupGet(x => x.ProviderName).Returns(_providerName);
        SetupGet(x => x.AllowsCaching).Returns(_allowsCaching);
        SetupGet(x => x.CacheTtl).Returns(_cacheTtl);

        Setup(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((address, _) =>
            {
                IncrementCallCount();
                ThrowQueuedExceptionIfAny();

                if (_geocodeResponses.TryGetValue(address, out var response))
                {
                    response.Provider = _providerName;
                    response.CacheHit = false;
                    return Task.FromResult(response);
                }

                // Generate default response
                var result = TestDataFactory.CreateGeocodeResult(address);
                result.Provider = _providerName;
                result.CacheHit = false;
                return Task.FromResult(result);
            });

        Setup(x => x.ReverseGeocodeAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
            .Returns<Coordinates, CancellationToken>((coordinates, _) =>
            {
                IncrementCallCount();
                ThrowQueuedExceptionIfAny();

                var key = $"{coordinates.Latitude},{coordinates.Longitude}";
                if (_geocodeResponses.TryGetValue(key, out var response))
                {
                    response.Provider = _providerName;
                    response.CacheHit = false;
                    return Task.FromResult(response);
                }

                // Generate default response
                var result = TestDataFactory.CreateGeocodeResult(coordinates: coordinates);
                result.Provider = _providerName;
                result.CacheHit = false;
                return Task.FromResult(result);
            });

        Setup(x => x.GetRouteAsync(It.IsAny<Coordinates>(), It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
            .Returns<Coordinates, Coordinates, CancellationToken>((from, to, _) =>
            {
                IncrementCallCount();
                ThrowQueuedExceptionIfAny();

                var key = $"{from.Latitude},{from.Longitude}|{to.Latitude},{to.Longitude}";
                if (_routeResponses.TryGetValue(key, out var response))
                {
                    response.Provider = _providerName;
                    response.CacheHit = false;
                    return Task.FromResult(response);
                }

                // Generate default response
                var result = TestDataFactory.CreateRouteResult(from, to);
                result.Provider = _providerName;
                result.CacheHit = false;
                return Task.FromResult(result);
            });

        Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(_isHealthy));
    }

    /// <summary>
    /// Pre-configure a geocode response for a specific address
    /// </summary>
    public void SetGeocodeResponse(string address, GeocodeResult response)
    {
        _geocodeResponses[address] = response;
    }

    /// <summary>
    /// Pre-configure a reverse geocode response for specific coordinates
    /// </summary>
    public void SetReverseGeocodeResponse(Coordinates coordinates, GeocodeResult response)
    {
        var key = $"{coordinates.Latitude},{coordinates.Longitude}";
        _geocodeResponses[key] = response;
    }

    /// <summary>
    /// Pre-configure a route response for specific coordinates
    /// </summary>
    public void SetRouteResponse(Coordinates from, Coordinates to, RouteResult response)
    {
        var key = $"{from.Latitude},{from.Longitude}|{to.Latitude},{to.Longitude}";
        _routeResponses[key] = response;
    }

    /// <summary>
    /// Queue an exception to be thrown on the next method call
    /// </summary>
    public void QueueException(Exception exception)
    {
        _exceptionsToThrow.Enqueue(exception);
    }

    /// <summary>
    /// Set the health status of this provider
    /// </summary>
    public void SetHealthy(bool isHealthy)
    {
        _isHealthy = isHealthy;
    }

    /// <summary>
    /// Get the total number of calls made to this provider
    /// </summary>
    public int CallCount => _callCount;

    /// <summary>
    /// Reset the call count
    /// </summary>
    public void ResetCallCount()
    {
        _callCount = 0;
    }

    /// <summary>
    /// Clear all pre-configured responses
    /// </summary>
    public void ClearResponses()
    {
        _geocodeResponses.Clear();
        _routeResponses.Clear();
        _exceptionsToThrow.Clear();
    }

    private void IncrementCallCount()
    {
        _callCount++;
    }

    private void ThrowQueuedExceptionIfAny()
    {
        if (_exceptionsToThrow.Count > 0)
        {
            throw _exceptionsToThrow.Dequeue();
        }
    }
}

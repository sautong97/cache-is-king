using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using CacheIsKing.Tests.TestData;
using Moq;

namespace CacheIsKing.Tests.Mocks;

/// <summary>
/// Mock implementation of ILocationService for testing the aggregation layer
/// </summary>
public class MockLocationService : Mock<ILocationService>
{
    private readonly Dictionary<string, GeocodeResult> _geocodeCache = new();
    private readonly Dictionary<string, RouteResult> _routeCache = new();
    private readonly Queue<Exception> _exceptionsToThrow = new();
    private int _callCount = 0;
    private bool _simulateCacheHit = false;

    public MockLocationService()
    {
        SetupMockBehavior();
    }

    private void SetupMockBehavior()
    {
        Setup(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((address, _) =>
            {
                IncrementCallCount();
                ThrowQueuedExceptionIfAny();

                if (_simulateCacheHit && _geocodeCache.TryGetValue(address, out var cachedResult))
                {
                    cachedResult.CacheHit = true;
                    cachedResult.ResponseTimeMs = 5; // Faster response for cache hit
                    return Task.FromResult(cachedResult);
                }

                var result = TestDataFactory.CreateGeocodeResult(address);
                result.CacheHit = _simulateCacheHit;
                
                // Store in cache for future hits
                _geocodeCache[address] = result;
                
                return Task.FromResult(result);
            });

        Setup(x => x.ReverseGeocodeAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
            .Returns<Coordinates, CancellationToken>((coordinates, _) =>
            {
                IncrementCallCount();
                ThrowQueuedExceptionIfAny();

                var key = $"{coordinates.Latitude},{coordinates.Longitude}";
                
                if (_simulateCacheHit && _geocodeCache.TryGetValue(key, out var cachedResult))
                {
                    cachedResult.CacheHit = true;
                    cachedResult.ResponseTimeMs = 5;
                    return Task.FromResult(cachedResult);
                }

                var result = TestDataFactory.CreateGeocodeResult(coordinates: coordinates);
                result.CacheHit = _simulateCacheHit;
                
                _geocodeCache[key] = result;
                
                return Task.FromResult(result);
            });

        Setup(x => x.GetRouteAsync(It.IsAny<Coordinates>(), It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
            .Returns<Coordinates, Coordinates, CancellationToken>((from, to, _) =>
            {
                IncrementCallCount();
                ThrowQueuedExceptionIfAny();

                var key = $"{from.Latitude},{from.Longitude}|{to.Latitude},{to.Longitude}";
                
                if (_simulateCacheHit && _routeCache.TryGetValue(key, out var cachedResult))
                {
                    cachedResult.CacheHit = true;
                    cachedResult.ResponseTimeMs = 8;
                    return Task.FromResult(cachedResult);
                }

                var result = TestDataFactory.CreateRouteResult(from, to);
                result.CacheHit = _simulateCacheHit;
                
                _routeCache[key] = result;
                
                return Task.FromResult(result);
            });

        Setup(x => x.GetProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                IncrementCallCount();
                ThrowQueuedExceptionIfAny();
                
                return Task.FromResult(TestDataFactory.CreateHealthStatus());
            });
    }

    /// <summary>
    /// Configure whether subsequent calls should simulate cache hits
    /// </summary>
    public void SimulateCacheHit(bool simulateHit = true)
    {
        _simulateCacheHit = simulateHit;
    }

    /// <summary>
    /// Pre-configure a geocode response for a specific address
    /// </summary>
    public void SetGeocodeResponse(string address, GeocodeResult response)
    {
        _geocodeCache[address] = response;
    }

    /// <summary>
    /// Pre-configure a route response for specific coordinates
    /// </summary>
    public void SetRouteResponse(Coordinates from, Coordinates to, RouteResult response)
    {
        var key = $"{from.Latitude},{from.Longitude}|{to.Latitude},{to.Longitude}";
        _routeCache[key] = response;
    }

    /// <summary>
    /// Queue an exception to be thrown on the next method call
    /// </summary>
    public void QueueException(Exception exception)
    {
        _exceptionsToThrow.Enqueue(exception);
    }

    /// <summary>
    /// Get the total number of calls made to this service
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
    /// Clear all cached responses
    /// </summary>
    public void ClearCache()
    {
        _geocodeCache.Clear();
        _routeCache.Clear();
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

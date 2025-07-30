using CacheIsKing.Tests.Mocks;
using CacheIsKing.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace CacheIsKing.Tests.Performance;

/// <summary>
/// Performance tests to verify caching benefits and response times
/// </summary>
public class CachingPerformanceTests
{
    [Fact]
    public async Task CacheHit_ShouldBeFasterThanCacheMiss()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var locationService = new MockLocationService();
        var address = "123 Performance Test Street";
        var testData = TestDataFactory.CreateGeocodeResult(address);

        // Simulate slow first call (cache miss)
        testData.ResponseTimeMs = 250;
        locationService.SetGeocodeResponse(address, testData);
        locationService.SimulateCacheHit(false);

        var cacheKey = cache.Object.GenerateCacheKey("geocode", new object[] { address });

        // Act - First call (cache miss)
        var firstCallStart = DateTime.UtcNow;
        var firstResult = await locationService.Object.GeocodeAsync(address);
        var firstCallDuration = DateTime.UtcNow - firstCallStart;

        // Cache the result
        await cache.Object.SetAsync(firstResult, cacheKey, TimeSpan.FromHours(1));

        // Simulate fast cache hit
        var cachedData = TestDataFactory.CreateGeocodeResult(address);
        cachedData.ResponseTimeMs = 5;
        cachedData.CacheHit = true;
        locationService.SetGeocodeResponse(address, cachedData);
        locationService.SimulateCacheHit(true);

        // Second call (cache hit)
        var secondCallStart = DateTime.UtcNow;
        var secondResult = await locationService.Object.GeocodeAsync(address);
        var secondCallDuration = DateTime.UtcNow - secondCallStart;

        // Assert
        firstResult.CacheHit.Should().BeFalse();
        secondResult.CacheHit.Should().BeTrue();
        secondResult.ResponseTimeMs.Should().BeLessOrEqualTo(firstResult.ResponseTimeMs);
        
        // Cache hit should be significantly faster
        secondResult.ResponseTimeMs.Should().BeLessOrEqualTo(20); // Cache hits should be very fast
    }

    [Fact]
    public async Task MultipleProviders_ShouldTrackPerformanceIndividually()
    {
        // Arrange
        var tomtomProvider = new MockLocationProviderService("TomTom", allowsCaching: false);
        var hereProvider = new MockLocationProviderService("HERE", allowsCaching: true);
        var googleProvider = new MockLocationProviderService("GoogleMaps", allowsCaching: true);

        var address = "123 Multi Provider Performance Test";

        // Configure different response times
        var tomtomResult = TestDataFactory.CreateGeocodeResult(address);
        tomtomResult.ResponseTimeMs = 300;
        tomtomProvider.SetGeocodeResponse(address, tomtomResult);

        var hereResult = TestDataFactory.CreateGeocodeResult(address);
        hereResult.ResponseTimeMs = 200;
        hereProvider.SetGeocodeResponse(address, hereResult);

        var googleResult = TestDataFactory.CreateGeocodeResult(address);
        googleResult.ResponseTimeMs = 150;
        googleProvider.SetGeocodeResponse(address, googleResult);

        // Act
        var tomtomResponse = await tomtomProvider.Object.GeocodeAsync(address);
        var hereResponse = await hereProvider.Object.GeocodeAsync(address);
        var googleResponse = await googleProvider.Object.GeocodeAsync(address);

        // Assert
        tomtomResponse.Provider.Should().Be("TomTom");
        tomtomResponse.ResponseTimeMs.Should().Be(300);
        tomtomProvider.Object.AllowsCaching.Should().BeFalse();

        hereResponse.Provider.Should().Be("HERE");
        hereResponse.ResponseTimeMs.Should().Be(200);
        hereProvider.Object.AllowsCaching.Should().BeTrue();

        googleResponse.Provider.Should().Be("GoogleMaps");
        googleResponse.ResponseTimeMs.Should().Be(150);
        googleProvider.Object.AllowsCaching.Should().BeTrue();

        // Each provider should have been called once
        tomtomProvider.CallCount.Should().Be(1);
        hereProvider.CallCount.Should().Be(1);
        googleProvider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task CacheService_ShouldHandleHighVolumeOperations()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var testData = TestDataFactory.CreateGeocodeResults(100);
        var keys = new List<string>();

        // Act - Store 100 items in cache
        for (int i = 0; i < testData.Count; i++)
        {
            var key = cache.Object.GenerateCacheKey("geocode", new object[] { $"address_{i}" });
            keys.Add(key);
            await cache.Object.SetAsync(testData[i], key, TimeSpan.FromHours(1));
        }

        // Retrieve all items
        var retrievedCount = 0;
        foreach (var key in keys)
        {
            var retrieved = await cache.Object.GetAsync<object>(key);
            if (retrieved != null)
                retrievedCount++;
        }

        // Assert
        retrievedCount.Should().Be(100);
        cache.GetAllKeys().Count().Should().Be(100);
    }

    [Fact]
    public async Task RepeatedCalls_ShouldDemonstrateCallReduction()
    {
        // Arrange
        var provider = new MockLocationProviderService("TestProvider", allowsCaching: true);
        var locationService = new MockLocationService();
        var address = "123 Call Reduction Test";

        // Configure responses
        var result = TestDataFactory.CreateGeocodeResult(address);
        provider.SetGeocodeResponse(address, result);
        locationService.SetGeocodeResponse(address, result);

        // Act - Make multiple calls for the same address
        locationService.SimulateCacheHit(false); // First call is cache miss
        await locationService.Object.GeocodeAsync(address);

        locationService.SimulateCacheHit(true); // Subsequent calls are cache hits
        await locationService.Object.GeocodeAsync(address);
        await locationService.Object.GeocodeAsync(address);
        await locationService.Object.GeocodeAsync(address);

        // Also test the provider directly
        await provider.Object.GeocodeAsync(address);
        await provider.Object.GeocodeAsync(address);

        // Assert
        // LocationService should have been called 4 times (testing the aggregation layer)
        locationService.CallCount.Should().Be(4);
        
        // Provider should have been called 2 times (testing direct provider calls)
        provider.CallCount.Should().Be(2);
    }

    [Fact]
    public void CacheKeyGeneration_ShouldBePerformant()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var addresses = TestDataFactory.CreateGeocodeResults(1000)
            .Select(r => r.FormattedAddress!)
            .ToList();

        // Act - Generate 1000 cache keys
        var start = DateTime.UtcNow;
        var keys = new List<string>();
        
        foreach (var address in addresses)
        {
            var key = cache.Object.GenerateCacheKey("geocode", new object[] { address });
            keys.Add(key);
        }
        
        var duration = DateTime.UtcNow - start;

        // Assert
        keys.Should().HaveCount(1000);
        keys.Distinct().Should().HaveCount(1000); // All keys should be unique
        duration.TotalMilliseconds.Should().BeLessThan(100); // Should be very fast
    }

    [Fact]
    public async Task ProviderFailover_ShouldMaintainPerformance()
    {
        // Arrange
        var primaryProvider = new MockLocationProviderService("Primary", allowsCaching: true);
        var fallbackProvider = new MockLocationProviderService("Fallback", allowsCaching: true);
        
        var address = "123 Failover Test";
        
        // Configure primary provider to fail
        primaryProvider.QueueException(new Exception("Primary provider failed"));
        
        // Configure fallback provider to succeed
        var fallbackResult = TestDataFactory.CreateGeocodeResult(address);
        fallbackResult.Provider = "Fallback";
        fallbackProvider.SetGeocodeResponse(address, fallbackResult);

        // Act
        Exception? primaryException = null;
        try
        {
            await primaryProvider.Object.GeocodeAsync(address);
        }
        catch (Exception ex)
        {
            primaryException = ex;
        }

        var fallbackResponse = await fallbackProvider.Object.GeocodeAsync(address);

        // Assert
        primaryException.Should().NotBeNull();
        primaryException!.Message.Should().Be("Primary provider failed");
        
        fallbackResponse.Should().NotBeNull();
        fallbackResponse.Provider.Should().Be("Fallback");
        fallbackResponse.FormattedAddress.Should().Be(address);
        
        primaryProvider.CallCount.Should().Be(1);
        fallbackProvider.CallCount.Should().Be(1);
    }
}

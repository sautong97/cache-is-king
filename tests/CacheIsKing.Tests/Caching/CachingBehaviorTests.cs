using CacheIsKing.Tests.Mocks;
using CacheIsKing.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace CacheIsKing.Tests.Caching;

/// <summary>
/// Integration tests for caching behavior across multiple service calls
/// </summary>
public class CachingBehaviorTests
{
    [Fact]
    public void MockHybridCacheService_ShouldStoreTAndRetrieveValues()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var testData = TestDataFactory.CreateGeocodeResult();
        var cacheKey = "test:geocode:123";

        // Act
        cache.Object.SetAsync(testData, cacheKey, TimeSpan.FromMinutes(30));
        var retrieved = cache.Object.GetAsync<object>(cacheKey).Result;

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().BeEquivalentTo(testData);
        cache.HasKey(cacheKey).Should().BeTrue();
    }

    [Fact]
    public void MockHybridCacheService_ShouldRespectExpiry()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var testData = TestDataFactory.CreateGeocodeResult();
        var cacheKey = "test:geocode:expiry";

        // Act
        cache.Object.SetAsync(testData, cacheKey, TimeSpan.FromSeconds(1));
        cache.ExpireKey(cacheKey); // Simulate expiry
        var retrieved = cache.Object.GetAsync<object>(cacheKey).Result;

        // Assert
        retrieved.Should().BeNull();
        cache.HasKey(cacheKey).Should().BeFalse();
    }

    [Fact]
    public void MockHybridCacheService_ShouldTrackAccessCount()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var testData = TestDataFactory.CreateGeocodeResult();
        var cacheKey = "test:geocode:access";

        // Act
        cache.Object.SetAsync(testData, cacheKey, TimeSpan.FromHours(1));
        cache.Object.GetAsync<object>(cacheKey).Wait();
        cache.Object.GetAsync<object>(cacheKey).Wait();
        cache.Object.GetAsync<object>(cacheKey).Wait();

        // Assert
        cache.GetAccessCount(cacheKey).Should().Be(3);
    }

    [Fact]
    public void MockHybridCacheService_ShouldGenerateConsistentKeys()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var address = "123 Main Street";

        // Act
        var key1 = cache.Object.GenerateCacheKey("geocode", new object[] { address });
        var key2 = cache.Object.GenerateCacheKey("geocode", new object[] { address });

        // Assert
        key1.Should().Be(key2);
        key1.Should().StartWith("geocode:");
    }

    [Fact]
    public void MockLocationProviderService_ShouldRespectCachingSettings()
    {
        // Arrange
        var provider1 = new MockLocationProviderService("TomTom", allowsCaching: false);
        var provider2 = new MockLocationProviderService("HERE", allowsCaching: true, TimeSpan.FromHours(6));

        // Assert
        provider1.Object.AllowsCaching.Should().BeFalse();
        provider1.Object.ProviderName.Should().Be("TomTom");
        
        provider2.Object.AllowsCaching.Should().BeTrue();
        provider2.Object.CacheTtl.Should().Be(TimeSpan.FromHours(6));
        provider2.Object.ProviderName.Should().Be("HERE");
    }

    [Fact]
    public async Task MockLocationProviderService_ShouldTrackCallCount()
    {
        // Arrange
        var provider = new MockLocationProviderService();
        var address = "123 Test Street";
        var coordinates = TestDataFactory.CreateCoordinates();

        // Act
        await provider.Object.GeocodeAsync(address);
        await provider.Object.ReverseGeocodeAsync(coordinates);
        await provider.Object.GetRouteAsync(coordinates, TestDataFactory.CreateCoordinates());

        // Assert
        provider.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task MockLocationProviderService_ShouldThrowQueuedExceptions()
    {
        // Arrange
        var provider = new MockLocationProviderService();
        var expectedException = new InvalidOperationException("Test exception");
        provider.QueueException(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.Object.GeocodeAsync("test address"));
        exception.Message.Should().Be("Test exception");
    }

    [Fact]
    public async Task CachingScenario_FirstCallMisses_SecondCallHits()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var locationService = new MockLocationService();
        var address = "123 Caching Test Street";
        
        // First call - cache miss
        locationService.SimulateCacheHit(false);
        var firstResult = await locationService.Object.GeocodeAsync(address);
        
        // Simulate caching the result
        var cacheKey = cache.Object.GenerateCacheKey("geocode", new object[] { address });
        await cache.Object.SetAsync(firstResult, cacheKey, TimeSpan.FromHours(1));
        
        // Second call - cache hit
        locationService.SimulateCacheHit(true);
        var secondResult = await locationService.Object.GeocodeAsync(address);

        // Assert
        firstResult.CacheHit.Should().BeFalse();
        secondResult.CacheHit.Should().BeTrue();
        secondResult.ResponseTimeMs.Should().BeLessOrEqualTo(firstResult.ResponseTimeMs);
        
        // Verify cache was accessed
        cache.GetAccessCount(cacheKey).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CachingScenario_MultipleProviders_ShouldTrackIndividualCalls()
    {
        // Arrange
        var tomtomProvider = new MockLocationProviderService("TomTom", allowsCaching: false);
        var hereProvider = new MockLocationProviderService("HERE", allowsCaching: true);
        var address = "123 Multi Provider Street";

        // Act - Call both providers
        await tomtomProvider.Object.GeocodeAsync(address);
        await hereProvider.Object.GeocodeAsync(address);
        await hereProvider.Object.GeocodeAsync(address); // Second call to HERE

        // Assert
        tomtomProvider.CallCount.Should().Be(1);
        hereProvider.CallCount.Should().Be(2);
        
        tomtomProvider.Object.AllowsCaching.Should().BeFalse();
        hereProvider.Object.AllowsCaching.Should().BeTrue();
    }

    [Fact]
    public void CacheKeyGeneration_ShouldBeConsistentForSameInputs()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var coordinates1 = new { lat = 40.7128, lon = -74.0060 };
        var coordinates2 = new { lat = 40.7128, lon = -74.0060 };

        // Act
        var key1 = cache.Object.GenerateCacheKey("route", new object[] { coordinates1.lat, coordinates1.lon });
        var key2 = cache.Object.GenerateCacheKey("route", new object[] { coordinates2.lat, coordinates2.lon });

        // Assert
        key1.Should().Be(key2);
    }

    [Fact]
    public void CacheKeyGeneration_ShouldBeDifferentForDifferentInputs()
    {
        // Arrange
        var cache = new MockHybridCacheService();

        // Act
        var key1 = cache.Object.GenerateCacheKey("geocode", new object[] { "123 Main St" });
        var key2 = cache.Object.GenerateCacheKey("geocode", new object[] { "456 Oak Ave" });
        var key3 = cache.Object.GenerateCacheKey("route", new object[] { "123 Main St" });

        // Assert
        key1.Should().NotBe(key2);
        key1.Should().NotBe(key3);
        key2.Should().NotBe(key3);
    }

    [Fact]
    public async Task ProviderHealthCheck_ShouldReturnConfiguredStatus()
    {
        // Arrange
        var healthyProvider = new MockLocationProviderService("HealthyProvider");
        var unhealthyProvider = new MockLocationProviderService("UnhealthyProvider");
        
        healthyProvider.SetHealthy(true);
        unhealthyProvider.SetHealthy(false);

        // Act
        var healthyResult = await healthyProvider.Object.IsHealthyAsync();
        var unhealthyResult = await unhealthyProvider.Object.IsHealthyAsync();

        // Assert
        healthyResult.Should().BeTrue();
        unhealthyResult.Should().BeFalse();
    }
}

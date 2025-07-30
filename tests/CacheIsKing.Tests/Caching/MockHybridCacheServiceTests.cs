using CacheIsKing.Core.Models;
using CacheIsKing.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace CacheIsKing.Tests.Caching;

/// <summary>
/// Tests for the MockHybridCacheService to verify caching behavior
/// </summary>
public class MockHybridCacheServiceTests
{
    [Fact]
    public async Task SetAsync_AndGetAsync_ShouldStoreAndRetrieveValue()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var testData = new GeocodeResult
        {
            FormattedAddress = "123 Test Street",
            Address = "123 Test Street",
            Coordinates = new Coordinates(40.7128, -74.0060),
            ProviderName = "TestProvider"
        };
        var cacheKey = "test:geocode:123";

        // Act
        await cache.SetAsync(cacheKey, testData, TimeSpan.FromMinutes(30));
        var retrieved = await cache.GetAsync<GeocodeResult>(cacheKey);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.FormattedAddress.Should().Be("123 Test Street");
        retrieved.Coordinates!.Latitude.Should().Be(40.7128);
        cache.HasKey(cacheKey).Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_WithExpiredKey_ShouldReturnNull()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var testData = new GeocodeResult
        {
            FormattedAddress = "Expired Test",
            ProviderName = "TestProvider"
        };
        var cacheKey = "test:geocode:expired";

        // Act
        await cache.SetAsync(cacheKey, testData, TimeSpan.FromSeconds(1));
        cache.ExpireKey(cacheKey); // Simulate expiry
        var retrieved = await cache.GetAsync<GeocodeResult>(cacheKey);

        // Assert
        retrieved.Should().BeNull();
        cache.HasKey(cacheKey).Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_ShouldTrackAccessCount()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var testData = new GeocodeResult
        {
            FormattedAddress = "Access Count Test",
            ProviderName = "TestProvider"
        };
        var cacheKey = "test:geocode:access";

        // Act
        await cache.SetAsync(cacheKey, testData, TimeSpan.FromHours(1));
        await cache.GetAsync<GeocodeResult>(cacheKey);
        await cache.GetAsync<GeocodeResult>(cacheKey);
        await cache.GetAsync<GeocodeResult>(cacheKey);

        // Assert
        cache.GetAccessCount(cacheKey).Should().Be(3);
    }

    [Fact]
    public void GenerateCacheKey_ShouldBeConsistentForSameInputs()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var address = "123 Main Street";

        // Act
        var key1 = cache.GenerateCacheKey("geocode", address);
        var key2 = cache.GenerateCacheKey("geocode", address);

        // Assert
        key1.Should().Be(key2);
        key1.Should().StartWith("geocode:");
    }

    [Fact]
    public void GenerateCacheKey_ShouldBeDifferentForDifferentInputs()
    {
        // Arrange
        var cache = new MockHybridCacheService();

        // Act
        var key1 = cache.GenerateCacheKey("geocode", "123 Main St");
        var key2 = cache.GenerateCacheKey("geocode", "456 Oak Ave");
        var key3 = cache.GenerateCacheKey("route", "123 Main St");

        // Assert
        key1.Should().NotBe(key2);
        key1.Should().NotBe(key3);
        key2.Should().NotBe(key3);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnCorrectStatus()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        var testData = new GeocodeResult
        {
            FormattedAddress = "Exists Test",
            ProviderName = "TestProvider"
        };
        var cacheKey = "test:geocode:exists";

        // Act & Assert
        var existsBeforeSet = await cache.ExistsAsync(cacheKey);
        existsBeforeSet.Should().BeFalse();

        await cache.SetAsync(cacheKey, testData, TimeSpan.FromHours(1));
        var existsAfterSet = await cache.ExistsAsync(cacheKey);
        existsAfterSet.Should().BeTrue();

        await cache.RemoveAsync(cacheKey);
        var existsAfterRemove = await cache.ExistsAsync(cacheKey);
        existsAfterRemove.Should().BeFalse();
    }

    [Fact]
    public void ClearCache_ShouldRemoveAllEntries()
    {
        // Arrange
        var cache = new MockHybridCacheService();
        
        // Act
        cache.SetAsync("key1", new GeocodeResult { ProviderName = "Test1" });
        cache.SetAsync("key2", new GeocodeResult { ProviderName = "Test2" });
        cache.GetAsync<GeocodeResult>("key1"); // Generate some access counts
        
        cache.ClearCache();

        // Assert
        cache.GetAllKeys().Should().BeEmpty();
        cache.GetAccessCount("key1").Should().Be(0);
        cache.HasKey("key1").Should().BeFalse();
        cache.HasKey("key2").Should().BeFalse();
    }
}

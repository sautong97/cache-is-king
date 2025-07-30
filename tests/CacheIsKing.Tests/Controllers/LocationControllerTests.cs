using CacheIsKing.Gateway.Controllers;
using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using CacheIsKing.Tests.Mocks;
using CacheIsKing.Tests.TestData;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CacheIsKing.Tests.Controllers;

/// <summary>
/// Unit tests for LocationController focusing on API behavior and caching mechanisms
/// </summary>
public class LocationControllerTests
{
    private readonly MockLocationService _mockLocationService;
    private readonly Mock<ILogger<LocationController>> _mockLogger;
    private readonly LocationController _controller;

    public LocationControllerTests()
    {
        _mockLocationService = new MockLocationService();
        _mockLogger = new Mock<ILogger<LocationController>>();
        _controller = new LocationController(_mockLocationService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GeocodeAsync_WithValidAddress_ReturnsOkResult()
    {
        // Arrange
        var address = "123 Main Street, New York, NY";
        var expectedResult = TestDataFactory.CreateGeocodeResult(address);
        _mockLocationService.SetGeocodeResponse(address, expectedResult);

        // Act
        var result = await _controller.GeocodeAsync(address);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var geocodeResult = okResult!.Value as GeocodeResult;
        geocodeResult.Should().NotBeNull();
        geocodeResult!.FormattedAddress.Should().Be(address);
        
        _mockLocationService.Verify(x => x.GeocodeAsync(address, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GeocodeAsync_WithEmptyAddress_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GeocodeAsync("");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Be("Address is required");
        
        _mockLocationService.Verify(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GeocodeAsync_WithNullCoordinatesResult_ReturnsNotFound()
    {
        // Arrange
        var address = "Invalid Address";
        var resultWithNullCoordinates = TestDataFactory.CreateGeocodeResult(address);
        resultWithNullCoordinates.Coordinates = null;
        _mockLocationService.SetGeocodeResponse(address, resultWithNullCoordinates);

        // Act
        var result = await _controller.GeocodeAsync(address);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be($"Could not geocode address: {address}");
    }

    [Fact]
    public async Task GeocodeAsync_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var address = "123 Main Street";
        _mockLocationService.QueueException(new Exception("Service unavailable"));

        // Act
        var result = await _controller.GeocodeAsync(address);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var errorResult = result.Result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
        errorResult.Value.Should().Be("Internal server error occurred while geocoding");
    }

    [Fact]
    public async Task GeocodeAsync_WithCacheHit_ReturnsFasterResponse()
    {
        // Arrange
        var address = "123 Cached Street";
        var cachedResult = TestDataFactory.CreateGeocodeResult(address);
        cachedResult.CacheHit = true;
        cachedResult.ResponseTimeMs = 5; // Simulate fast cache response
        
        _mockLocationService.SetGeocodeResponse(address, cachedResult);
        _mockLocationService.SimulateCacheHit(true);

        // Act
        var result = await _controller.GeocodeAsync(address);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var geocodeResult = okResult!.Value as GeocodeResult;
        geocodeResult!.CacheHit.Should().BeTrue();
        geocodeResult.ResponseTimeMs.Should().BeLessOrEqualTo(10); // Cache should be fast
    }

    [Fact]
    public async Task ReverseGeocodeAsync_WithValidCoordinates_ReturnsOkResult()
    {
        // Arrange
        var coordinates = TestDataFactory.CreateCoordinates();
        var expectedResult = TestDataFactory.CreateGeocodeResult(coordinates: coordinates);
        _mockLocationService.SetGeocodeResponse($"{coordinates.Latitude},{coordinates.Longitude}", expectedResult);

        // Act
        var result = await _controller.ReverseGeocodeAsync(coordinates.Latitude, coordinates.Longitude);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var geocodeResult = okResult!.Value as GeocodeResult;
        geocodeResult.Should().NotBeNull();
        geocodeResult!.Coordinates.Should().BeEquivalentTo(coordinates);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_WithInvalidCoordinates_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ReverseGeocodeAsync(91, 181); // Invalid coordinates

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().Contain("Invalid coordinates");
        
        _mockLocationService.Verify(x => x.ReverseGeocodeAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRouteAsync_WithValidCoordinates_ReturnsOkResult()
    {
        // Arrange
        var from = TestDataFactory.CreateCoordinates();
        var to = TestDataFactory.CreateCoordinates();
        var expectedResult = TestDataFactory.CreateRouteResult(from, to);
        _mockLocationService.SetRouteResponse(from, to, expectedResult);

        // Act
        var result = await _controller.GetRouteAsync(from.Latitude, from.Longitude, to.Latitude, to.Longitude);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var routeResult = okResult!.Value as RouteResult;
        routeResult.Should().NotBeNull();
        routeResult!.From.Should().BeEquivalentTo(from);
        routeResult.To.Should().BeEquivalentTo(to);
        routeResult.DistanceMeters.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRouteAsync_WithZeroDistance_ReturnsNotFound()
    {
        // Arrange
        var from = TestDataFactory.CreateCoordinates();
        var to = TestDataFactory.CreateCoordinates();
        var resultWithZeroDistance = TestDataFactory.CreateRouteResult(from, to);
        resultWithZeroDistance.DistanceMeters = 0;
        _mockLocationService.SetRouteResponse(from, to, resultWithZeroDistance);

        // Act
        var result = await _controller.GetRouteAsync(from.Latitude, from.Longitude, to.Latitude, to.Longitude);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetHealthAsync_ReturnsOkWithHealthStatus()
    {
        // Act
        var result = await _controller.GetHealthAsync();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var healthStatus = okResult!.Value as Dictionary<string, bool>;
        healthStatus.Should().NotBeNull();
        healthStatus.Should().NotBeEmpty();
        
        _mockLocationService.Verify(x => x.GetProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHealthAsync_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockLocationService.QueueException(new Exception("Health check failed"));

        // Act
        var result = await _controller.GetHealthAsync();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var errorResult = result.Result as ObjectResult;
        errorResult!.StatusCode.Should().Be(500);
    }

    [Theory]
    [InlineData(-91, 0)] // Invalid latitude
    [InlineData(91, 0)]  // Invalid latitude
    [InlineData(0, -181)] // Invalid longitude
    [InlineData(0, 181)]  // Invalid longitude
    public async Task ReverseGeocodeAsync_WithInvalidCoordinatesValues_ReturnsBadRequest(double lat, double lon)
    {
        // Act
        var result = await _controller.ReverseGeocodeAsync(lat, lon);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData(-91, 0, 0, 0)] // Invalid from latitude
    [InlineData(0, -181, 0, 0)] // Invalid from longitude
    [InlineData(0, 0, 91, 0)]   // Invalid to latitude
    [InlineData(0, 0, 0, 181)]  // Invalid to longitude
    public async Task GetRouteAsync_WithInvalidCoordinatesValues_ReturnsBadRequest(
        double fromLat, double fromLon, double toLat, double toLon)
    {
        // Act
        var result = await _controller.GetRouteAsync(fromLat, fromLon, toLat, toLon);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task MultipleCalls_ToSameAddress_ShouldDemonstrateCaching()
    {
        // Arrange
        var address = "123 Cache Test Street";
        var initialResult = TestDataFactory.CreateGeocodeResult(address);
        _mockLocationService.SetGeocodeResponse(address, initialResult);

        // First call (cache miss)
        _mockLocationService.SimulateCacheHit(false);
        var firstResult = await _controller.GeocodeAsync(address);
        
        // Second call (cache hit)
        _mockLocationService.SimulateCacheHit(true);
        var secondResult = await _controller.GeocodeAsync(address);

        // Assert
        firstResult.Result.Should().BeOfType<OkObjectResult>();
        secondResult.Result.Should().BeOfType<OkObjectResult>();
        
        var firstGeocode = (firstResult.Result as OkObjectResult)!.Value as GeocodeResult;
        var secondGeocode = (secondResult.Result as OkObjectResult)!.Value as GeocodeResult;
        
        firstGeocode!.CacheHit.Should().BeFalse();
        secondGeocode!.CacheHit.Should().BeTrue();
        secondGeocode.ResponseTimeMs.Should().BeLessOrEqualTo(firstGeocode.ResponseTimeMs);
    }
}

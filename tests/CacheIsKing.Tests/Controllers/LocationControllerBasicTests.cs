using CacheIsKing.Gateway.Controllers;
using CacheIsKing.Core.Interfaces;
using CacheIsKing.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CacheIsKing.Tests.Controllers;

/// <summary>
/// Basic unit tests for LocationController
/// </summary>
public class LocationControllerBasicTests
{
    private readonly Mock<ILocationService> _mockLocationService;
    private readonly Mock<ILogger<LocationController>> _mockLogger;
    private readonly LocationController _controller;

    public LocationControllerBasicTests()
    {
        _mockLocationService = new Mock<ILocationService>();
        _mockLogger = new Mock<ILogger<LocationController>>();
        _controller = new LocationController(_mockLocationService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GeocodeAsync_WithValidAddress_ReturnsOkResult()
    {
        // Arrange
        var address = "123 Main Street, New York, NY";
        var expectedResult = new GeocodeResult
        {
            FormattedAddress = address,
            Address = address,
            Coordinates = new Coordinates(40.7128, -74.0060),
            Confidence = 0.95,
            ProviderName = "TomTom"
        };
        
        _mockLocationService
            .Setup(x => x.GeocodeAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

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
        var resultWithNullCoordinates = new GeocodeResult
        {
            FormattedAddress = address,
            Address = address,
            Coordinates = null,
            ProviderName = "TomTom"
        };
        
        _mockLocationService
            .Setup(x => x.GeocodeAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultWithNullCoordinates);

        // Act
        var result = await _controller.GeocodeAsync(address);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().Be($"Could not geocode address: {address}");
    }

    [Fact]
    public async Task ReverseGeocodeAsync_WithValidCoordinates_ReturnsOkResult()
    {
        // Arrange
        var coordinates = new Coordinates(40.7128, -74.0060);
        var expectedResult = new GeocodeResult
        {
            FormattedAddress = "New York, NY",
            Address = "New York, NY",
            Coordinates = coordinates,
            ProviderName = "TomTom"
        };
        
        _mockLocationService
            .Setup(x => x.ReverseGeocodeAsync(It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ReverseGeocodeAsync(coordinates.Latitude, coordinates.Longitude);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var geocodeResult = okResult!.Value as GeocodeResult;
        geocodeResult.Should().NotBeNull();
        geocodeResult!.Coordinates.Should().BeEquivalentTo(coordinates);
    }

    [Theory]
    [InlineData(-91, 0)] // Invalid latitude
    [InlineData(91, 0)]  // Invalid latitude
    [InlineData(0, -181)] // Invalid longitude
    [InlineData(0, 181)]  // Invalid longitude
    public async Task ReverseGeocodeAsync_WithInvalidCoordinates_ReturnsBadRequest(double lat, double lon)
    {
        // Act
        var result = await _controller.ReverseGeocodeAsync(lat, lon);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetRouteAsync_WithValidCoordinates_ReturnsOkResult()
    {
        // Arrange
        var from = new Coordinates(40.7128, -74.0060);
        var to = new Coordinates(34.0522, -118.2437);
        var expectedResult = new RouteResult
        {
            Origin = from,
            Destination = to,
            DistanceMeters = 4000000,
            Duration = TimeSpan.FromHours(5),
            ProviderName = "TomTom"
        };
        
        _mockLocationService
            .Setup(x => x.GetRouteAsync(It.IsAny<Coordinates>(), It.IsAny<Coordinates>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetRouteAsync(from.Latitude, from.Longitude, to.Latitude, to.Longitude);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var routeResult = okResult!.Value as RouteResult;
        routeResult.Should().NotBeNull();
        routeResult!.Origin.Should().BeEquivalentTo(from);
        routeResult.Destination.Should().BeEquivalentTo(to);
        routeResult.DistanceMeters.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetHealthAsync_ReturnsOkWithHealthStatus()
    {
        // Arrange
        var healthStatus = new Dictionary<string, bool>
        {
            { "TomTom", true },
            { "HERE", false }
        };
        
        _mockLocationService
            .Setup(x => x.GetProvidersHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthStatus);

        // Act
        var result = await _controller.GetHealthAsync();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedHealthStatus = okResult!.Value as Dictionary<string, bool>;
        returnedHealthStatus.Should().NotBeNull();
        returnedHealthStatus.Should().BeEquivalentTo(healthStatus);
        
        _mockLocationService.Verify(x => x.GetProvidersHealthAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

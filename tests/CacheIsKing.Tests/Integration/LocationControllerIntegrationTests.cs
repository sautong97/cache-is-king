using CacheIsKing.Core.Models;
using CacheIsKing.Tests.Integration;
using CacheIsKing.Tests.TestData;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CacheIsKing.Tests.Integration;

/// <summary>
/// Integration tests for the LocationController API endpoints
/// Testing the full request/response pipeline with mocked external dependencies
/// </summary>
public class LocationControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LocationControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_Geocode_WithValidAddress_ReturnsSuccessAndCorrectResult()
    {
        // Arrange
        _factory.ResetMocks();
        var address = "123 Integration Test Street";
        var expectedResult = TestDataFactory.CreateGeocodeResult(address);
        _factory.MockLocationService.SetGeocodeResponse(address, expectedResult);

        // Act
        var response = await _client.GetAsync($"/api/Location/geocode?address={Uri.EscapeDataString(address)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<GeocodeResult>();
        result.Should().NotBeNull();
        result!.FormattedAddress.Should().Be(address);
        result.Coordinates.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_Geocode_WithEmptyAddress_ReturnsBadRequest()
    {
        // Arrange
        _factory.ResetMocks();

        // Act
        var response = await _client.GetAsync("/api/Location/geocode?address=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Address is required");
    }

    [Fact]
    public async Task GET_ReverseGeocode_WithValidCoordinates_ReturnsSuccessAndCorrectResult()
    {
        // Arrange
        _factory.ResetMocks();
        var coordinates = TestDataFactory.CreateCoordinates();
        var expectedResult = TestDataFactory.CreateGeocodeResult(coordinates: coordinates);
        var key = $"{coordinates.Latitude},{coordinates.Longitude}";
        _factory.MockLocationService.SetGeocodeResponse(key, expectedResult);

        // Act
        var response = await _client.GetAsync(
            $"/api/Location/reverse-geocode?latitude={coordinates.Latitude}&longitude={coordinates.Longitude}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<GeocodeResult>();
        result.Should().NotBeNull();
        result!.Coordinates.Should().BeEquivalentTo(coordinates);
    }

    [Fact]
    public async Task GET_ReverseGeocode_WithInvalidCoordinates_ReturnsBadRequest()
    {
        // Arrange
        _factory.ResetMocks();

        // Act
        var response = await _client.GetAsync("/api/Location/reverse-geocode?latitude=91&longitude=181");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid coordinates");
    }

    [Fact]
    public async Task GET_Route_WithValidCoordinates_ReturnsSuccessAndCorrectResult()
    {
        // Arrange
        _factory.ResetMocks();
        var from = TestDataFactory.CreateCoordinates();
        var to = TestDataFactory.CreateCoordinates();
        var expectedResult = TestDataFactory.CreateRouteResult(from, to);
        _factory.MockLocationService.SetRouteResponse(from, to, expectedResult);

        // Act
        var response = await _client.GetAsync(
            $"/api/Location/route?fromLat={from.Latitude}&fromLon={from.Longitude}&toLat={to.Latitude}&toLon={to.Longitude}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<RouteResult>();
        result.Should().NotBeNull();
        result!.From.Should().BeEquivalentTo(from);
        result.To.Should().BeEquivalentTo(to);
        result.DistanceMeters.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_Route_WithInvalidCoordinates_ReturnsBadRequest()
    {
        // Arrange
        _factory.ResetMocks();

        // Act
        var response = await _client.GetAsync("/api/Location/route?fromLat=91&fromLon=0&toLat=0&toLon=181");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid coordinates");
    }

    [Fact]
    public async Task GET_Health_ReturnsSuccessAndHealthStatus()
    {
        // Arrange
        _factory.ResetMocks();

        // Act
        var response = await _client.GetAsync("/api/Location/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Geocode_CachingBehavior_FirstCallMiss_SecondCallHit()
    {
        // Arrange
        _factory.ResetMocks();
        var address = "123 Caching Integration Test";
        var result = TestDataFactory.CreateGeocodeResult(address);
        _factory.MockLocationService.SetGeocodeResponse(address, result);

        // First call - should be cache miss
        _factory.MockLocationService.SimulateCacheHit(false);
        var firstResponse = await _client.GetAsync($"/api/Location/geocode?address={Uri.EscapeDataString(address)}");
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<GeocodeResult>();

        // Second call - should be cache hit
        _factory.MockLocationService.SimulateCacheHit(true);
        var secondResponse = await _client.GetAsync($"/api/Location/geocode?address={Uri.EscapeDataString(address)}");
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<GeocodeResult>();

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        firstResult!.CacheHit.Should().BeFalse();
        secondResult!.CacheHit.Should().BeTrue();
        secondResult.ResponseTimeMs.Should().BeLessOrEqualTo(firstResult.ResponseTimeMs);
        
        // Verify service was called twice
        _factory.MockLocationService.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task MultipleEndpoints_ShouldWorkIndependently()
    {
        // Arrange
        _factory.ResetMocks();
        var address = "123 Multi Test Street";
        var coordinates = TestDataFactory.CreateCoordinates();
        var from = TestDataFactory.CreateCoordinates();
        var to = TestDataFactory.CreateCoordinates();

        var geocodeResult = TestDataFactory.CreateGeocodeResult(address);
        var reverseGeocodeResult = TestDataFactory.CreateGeocodeResult(coordinates: coordinates);
        var routeResult = TestDataFactory.CreateRouteResult(from, to);

        _factory.MockLocationService.SetGeocodeResponse(address, geocodeResult);
        _factory.MockLocationService.SetGeocodeResponse($"{coordinates.Latitude},{coordinates.Longitude}", reverseGeocodeResult);
        _factory.MockLocationService.SetRouteResponse(from, to, routeResult);

        // Act - Call all endpoints
        var geocodeResponse = await _client.GetAsync($"/api/Location/geocode?address={Uri.EscapeDataString(address)}");
        var reverseGeocodeResponse = await _client.GetAsync(
            $"/api/Location/reverse-geocode?latitude={coordinates.Latitude}&longitude={coordinates.Longitude}");
        var routeResponse = await _client.GetAsync(
            $"/api/Location/route?fromLat={from.Latitude}&fromLon={from.Longitude}&toLat={to.Latitude}&toLon={to.Longitude}");
        var healthResponse = await _client.GetAsync("/api/Location/health");

        // Assert
        geocodeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        reverseGeocodeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        routeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.MockLocationService.CallCount.Should().Be(4); // All four endpoints called
    }

    [Fact]
    public async Task ServiceException_ShouldReturn500Error()
    {
        // Arrange
        _factory.ResetMocks();
        var address = "Exception Test Address";
        _factory.MockLocationService.QueueException(new Exception("Simulated service failure"));

        // Act
        var response = await _client.GetAsync($"/api/Location/geocode?address={Uri.EscapeDataString(address)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Internal server error occurred while geocoding");
    }

    [Theory]
    [InlineData("/api/Location/geocode?address=Test%20Address")]
    [InlineData("/api/Location/reverse-geocode?latitude=40.7128&longitude=-74.0060")]
    [InlineData("/api/Location/route?fromLat=40.7128&fromLon=-74.0060&toLat=34.0522&toLon=-118.2437")]
    [InlineData("/api/Location/health")]
    public async Task AllEndpoints_ShouldReturnValidJsonResponse(string endpoint)
    {
        // Arrange
        _factory.ResetMocks();

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var jsonDocument = JsonDocument.Parse(content);
        jsonDocument.Should().NotBeNull();
    }
}

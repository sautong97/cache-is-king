# CacheIsKing Tests

This test project provides comprehensive testing for the CacheIsKing API gateway, focusing on caching mechanisms and API controller behavior without making actual third-party API calls.

## Test Structure

### 📁 Controllers/
- **LocationControllerTests.cs**: Unit tests for the LocationController API endpoints
  - Tests all API endpoints (geocode, reverse-geocode, route, health)
  - Validates request/response handling
  - Tests error scenarios and validation
  - Verifies caching behavior simulation

### 📁 Mocks/
- **MockHybridCacheService.cs**: Mock implementation of the hybrid caching layer
  - Simulates both in-memory and distributed cache behavior
  - Tracks cache hits, misses, and access patterns
  - Supports cache expiry and key generation testing

- **MockLocationProviderService.cs**: Mock for third-party location providers
  - Simulates different providers (TomTom, HERE, GoogleMaps)
  - Configurable caching policies per provider
  - Exception simulation and health status control

- **MockLocationService.cs**: Mock for the aggregation layer
  - Simulates cache hits/misses
  - Tracks service call patterns
  - Configurable responses and error scenarios

### 📁 Integration/
- **TestWebApplicationFactory.cs**: Custom test factory for integration tests
- **LocationControllerIntegrationTests.cs**: Full API pipeline tests
  - Tests complete HTTP request/response cycles
  - Validates JSON serialization
  - Tests caching behavior across multiple requests

### 📁 Caching/
- **CachingBehaviorTests.cs**: Tests for caching mechanisms
  - Cache storage and retrieval
  - Cache expiry behavior
  - Provider-specific caching policies
  - Cache key generation and consistency

### 📁 Performance/
- **CachingPerformanceTests.cs**: Performance-focused tests
  - Cache hit vs cache miss performance
  - Provider failover scenarios
  - High-volume operation handling
  - Call reduction verification

### 📁 TestData/
- **TestDataFactory.cs**: Generates realistic test data using Bogus library
  - Creates test objects for all domain models
  - Provides consistent test data across tests

## Key Testing Strategies

### 🎯 Caching Simulation
The tests simulate caching behavior without actual Redis or external APIs:

```csharp
// Simulate cache miss on first call
mockService.SimulateCacheHit(false);
var firstResult = await controller.GeocodeAsync("address");

// Simulate cache hit on second call
mockService.SimulateCacheHit(true);
var secondResult = await controller.GeocodeAsync("address");

// Verify caching behavior
firstResult.CacheHit.Should().BeFalse();
secondResult.CacheHit.Should().BeTrue();
```

### 🔄 Provider Behavior Testing
Tests different provider configurations:

```csharp
// TomTom doesn't allow caching
var tomtom = new MockLocationProviderService("TomTom", allowsCaching: false);

// HERE allows caching for 6 hours
var here = new MockLocationProviderService("HERE", allowsCaching: true, TimeSpan.FromHours(6));
```

### 📊 Call Tracking
Verify external API call reduction:

```csharp
provider.CallCount.Should().Be(1); // Only one external call despite multiple requests
```

### 🏗️ Integration Testing
Full API pipeline testing:

```csharp
var response = await client.GetAsync("/api/Location/geocode?address=test");
response.StatusCode.Should().Be(HttpStatusCode.OK);
```

## Test Categories

### ✅ Unit Tests
- Individual component testing
- Mock dependency injection
- Isolated behavior verification

### 🔗 Integration Tests
- Full API pipeline testing
- HTTP request/response validation
- End-to-end caching simulation

### ⚡ Performance Tests
- Cache hit/miss timing
- Provider failover scenarios
- High-volume operation testing

## Running Tests

### All Tests
```bash
dotnet test
```

### Specific Test Category
```bash
dotnet test --filter "FullyQualifiedName~Controllers"
dotnet test --filter "FullyQualifiedName~Integration"
dotnet test --filter "FullyQualifiedName~Performance"
```

### With Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Key Benefits

1. **No External Dependencies**: Tests run without Redis or third-party APIs
2. **Fast Execution**: All tests use in-memory mocks
3. **Predictable Results**: Controlled test data and responses
4. **Comprehensive Coverage**: Tests all aspects of caching behavior
5. **Performance Validation**: Verifies caching performance benefits
6. **Realistic Scenarios**: Simulates real-world provider differences and failures

## Mock Capabilities

### Cache Service Mock
- ✅ In-memory cache simulation
- ✅ Cache expiry handling
- ✅ Access count tracking
- ✅ Key generation testing
- ✅ Cache miss/hit simulation

### Provider Service Mock
- ✅ Multiple provider simulation
- ✅ Provider-specific caching policies
- ✅ Exception handling and failover
- ✅ Call count tracking
- ✅ Health status simulation

### Location Service Mock
- ✅ Aggregation layer testing
- ✅ Cache behavior simulation
- ✅ Provider routing logic
- ✅ Response time simulation
- ✅ Error scenario testing

This test design ensures that your caching mechanisms work as intended while providing fast, reliable tests that don't depend on external services.

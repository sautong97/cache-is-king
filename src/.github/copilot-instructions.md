<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Cache Is King - Location Services API

This is a .NET solution implementing a scalable and cost-efficient third-party API integration layer for location services.

## Architecture Guidelines

- **Core Project**: Contains shared interfaces, models, and utilities
- **Caching Project**: Implements hybrid caching (in-memory + Redis) with provider-specific policies
- **Providers Project**: Contains service connectors for TomTom, HERE, and other location providers
- **Aggregation Project**: Orchestrates multiple providers with fallback and caching logic
- **Gateway Project**: ASP.NET Core Web API serving as the public interface

## Key Design Principles

1. **Provider Abstraction**: All providers implement `ILocationProviderService`
2. **Caching Strategy**: Respect provider-specific caching policies (e.g., TomTom prohibits server-side caching)
3. **Fallback Logic**: Try providers in priority order with graceful degradation
4. **Circuit Breaker Pattern**: Use Polly for resilience and retry logic
5. **Dependency Injection**: All services are registered through DI container

## Provider-Specific Considerations

- **TomTom**: No server-side caching allowed per terms of service
- **HERE**: Caching allowed with 6-hour TTL
- **Google Maps**: Caching allowed with 24-hour TTL

## Cache Key Strategy

Use `CacheKeyGenerator` utility for consistent key generation:
- Geocode: `geocode:{hash}`
- Reverse Geocode: `reverse:{lat}:{lon}`
- Route: `route:{fromLat}:{fromLon}:{toLat}:{toLon}`

## Error Handling

- Controllers return appropriate HTTP status codes
- Services log errors and continue with fallback providers
- Failed requests result in empty results with provider name "None"

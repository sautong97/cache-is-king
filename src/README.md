# Cache Is King - Location Services API

A scalable and cost-efficient third-party API integration layer for location services, built with an AI-first approach.

## 🏗️ Architecture

This solution implements a hybrid caching layer that integrates with multiple location service providers (TomTom, HERE, Google Maps) while minimizing external API calls and respecting provider-specific caching policies.

### Components

- **CacheIsKing.Core**: Shared interfaces, models, and utilities
- **CacheIsKing.Caching**: Hybrid caching implementation (in-memory + Redis)
- **CacheIsKing.Providers**: Service connectors for location providers
- **CacheIsKing.Aggregation**: Business logic and provider orchestration
- **CacheIsKing.Gateway**: ASP.NET Core Web API gateway

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- Optional: Redis instance for distributed caching

### Configuration

1. Update `appsettings.json` in the Gateway project:

```json
{
  "ConnectionStrings": {
    "Redis": "your-redis-connection-string"
  },
  "ApiKeys": {
    "TomTom": "your-tomtom-api-key",
    "HERE": "your-here-api-key"
  }
}
```

### Running the Application

```bash
cd CacheIsKing.Gateway
dotnet run
```

The API will be available at `https://localhost:5001` (or the port shown in console).

## 📚 API Endpoints

### Geocoding
```http
GET /api/location/geocode?address=123 Main St, London
```

### Reverse Geocoding
```http
GET /api/location/reverse-geocode?latitude=51.5074&longitude=-0.1278
```

### Routing
```http
GET /api/location/route?fromLat=51.5074&fromLon=-0.1278&toLat=40.7128&toLon=-74.0060
```

### Health Check
```http
GET /api/location/health
```

## 🧩 Key Features

- **Hybrid Caching**: Two-tier caching with in-memory and Redis
- **Provider Fallback**: Automatic failover between location service providers
- **Compliance**: Respects provider-specific caching policies
- **Cost Optimization**: Minimizes external API calls through intelligent caching
- **Scalability**: Designed for multi-instance deployments

## 🔧 Caching Strategy

| Provider | Caching Allowed | TTL | Reason |
|----------|----------------|-----|---------|
| TomTom | ❌ | N/A | Terms of Service prohibit server-side caching |
| HERE | ✅ | 6 hours | Standard caching policy |
| Google Maps | ✅ | 24 hours | Extended caching allowed |

## 🛠️ Development

### Building the Solution

```bash
dotnet build CacheIsKing.sln
```

### Running Tests

```bash
dotnet test
```

## 📈 Performance Benefits

- Up to **70% reduction in external API calls** for cacheable providers
- Sub-millisecond response times for cached results
- Shared cache benefits across multiple application instances
- Automatic negative caching to prevent repeated failed requests

## 🔐 Security

- API keys stored in configuration (use Azure Key Vault in production)
- HTTPS enforced
- Input validation on all endpoints
- Rate limiting ready (implement based on requirements)

## 🚀 Deployment

The solution is designed for deployment on:
- Azure App Service
- Azure Kubernetes Service (AKS)
- Docker containers
- On-premises servers

For production, consider:
- Azure Redis Cache for distributed caching
- Azure Key Vault for credential management
- Application Insights for monitoring
- Azure API Management for advanced features

## 🤝 Contributors

1. Tan Sau Tong
2. Jason Hew

## 📝 License

This project is built using an AI-first approach for learning and development purposes.

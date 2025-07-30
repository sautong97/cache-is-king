using CacheIsKing.Gateway;
using CacheIsKing.Core.Interfaces;
using CacheIsKing.Tests.Mocks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CacheIsKing.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration testing with mocked dependencies
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public MockLocationService MockLocationService { get; } = new();
    public MockHybridCacheService MockCacheService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real services
            services.RemoveAll(typeof(ILocationService));
            services.RemoveAll(typeof(IHybridCacheService));

            // Add our mocks
            services.AddSingleton(MockLocationService.Object);
            services.AddSingleton(MockCacheService.Object);

            // Configure logging for tests
            services.AddLogging();
        });

        builder.UseEnvironment("Testing");
    }

    public void ResetMocks()
    {
        MockLocationService.ClearCache();
        MockLocationService.ResetCallCount();
        MockLocationService.SimulateCacheHit(false);
        
        MockCacheService.ClearCache();
    }
}

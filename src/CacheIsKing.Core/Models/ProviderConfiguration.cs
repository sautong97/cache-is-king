namespace CacheIsKing.Core.Models;

/// <summary>
/// Configuration for a location service provider
/// </summary>
public class ProviderConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool AllowsCaching { get; set; } = true;
    public TimeSpan? CacheTtl { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RateLimitPerMinute { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; } = 0; // Higher number = higher priority
}

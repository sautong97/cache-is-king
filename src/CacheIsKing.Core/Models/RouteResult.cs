namespace CacheIsKing.Core.Models;

/// <summary>
/// Result of a routing operation
/// </summary>
public class RouteResult
{
    public Coordinates Origin { get; set; } = new(0, 0);
    public Coordinates Destination { get; set; } = new(0, 0);
    public double DistanceMeters { get; set; }
    public TimeSpan Duration { get; set; }
    public List<Coordinates> RoutePoints { get; set; } = new();
    public string Instructions { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}

namespace CacheIsKing.Core.Models;

/// <summary>
/// Represents geographical coordinates
/// </summary>
public record Coordinates(double Latitude, double Longitude)
{
    public override string ToString() => $"{Latitude},{Longitude}";
}

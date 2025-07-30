using System.Security.Cryptography;
using System.Text;

namespace CacheIsKing.Core.Utilities;

/// <summary>
/// Utility class for generating cache keys
/// </summary>
public static class CacheKeyGenerator
{
    /// <summary>
    /// Generate a hashed cache key from multiple parameters
    /// </summary>
    public static string GenerateKey(string prefix, params object[] parameters)
    {
        var combined = string.Join(":", parameters.Select(p => p?.ToString() ?? "null"));
        var hash = ComputeSha256Hash(combined);
        return $"{prefix}:{hash}";
    }
    
    /// <summary>
    /// Generate cache key for geocoding
    /// </summary>
    public static string ForGeocode(string address) => GenerateKey("geocode", address.ToLowerInvariant().Trim());
    
    /// <summary>
    /// Generate cache key for reverse geocoding
    /// </summary>
    public static string ForReverseGeocode(double latitude, double longitude) => 
        GenerateKey("reverse", $"{latitude:F6}", $"{longitude:F6}");
    
    /// <summary>
    /// Generate cache key for routing
    /// </summary>
    public static string ForRoute(double fromLat, double fromLon, double toLat, double toLon) =>
        GenerateKey("route", $"{fromLat:F6}", $"{fromLon:F6}", $"{toLat:F6}", $"{toLon:F6}");
    
    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16]; // Take first 16 chars for shorter keys
    }
}

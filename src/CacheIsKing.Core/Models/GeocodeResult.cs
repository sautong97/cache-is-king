namespace CacheIsKing.Core.Models;

/// <summary>
/// Result of a geocoding operation
/// </summary>
public class GeocodeResult
{
    public string Address { get; set; } = string.Empty;
    public Coordinates? Coordinates { get; set; }
    public double Confidence { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}

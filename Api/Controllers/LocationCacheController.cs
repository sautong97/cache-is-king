using Api.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationCacheController : ControllerBase
{
    private readonly IRedisService _redisService;

    public LocationCacheController(IRedisService redisService)
    {
        _redisService = redisService;
    }

    [HttpPost("set")]
    public async Task<IActionResult> SetLocation([FromBody] LocationData data)
    {
        var key = $"location:address:{data.AddressHash}";
        await _redisService.SetObjectAsync(key, data, TimeSpan.FromHours(6));
        return Ok("Location cached.");
    }

    [HttpGet("get/{addressHash}")]
    public async Task<IActionResult> GetLocation(string addressHash)
    {
        var key = $"location:address:{addressHash}";
        var result = await _redisService.GetObjectAsync<LocationData>(key);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}

public class LocationData
{
    public string AddressHash { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

using StackExchange.Redis;
using System.Text.Json;

namespace Api.Infrastructure.Services;

public interface IRedisService : IDisposable
{
    Task SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetObjectAsync<T>(string key);
    Task SetObjectsAsync<T>(Dictionary<string, T> items, TimeSpan? expiry = null);
    Task<Dictionary<string, T?>> GetObjectsAsync<T>(IEnumerable<string> keys);
    Task<bool> RemoveAsync(string key);
}

public class RedisService : IRedisService
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(string connectionString = "localhost:6379")
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    // Store any object as JSON
    public async Task SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    // Retrieve and deserialize object
    public async Task<T?> GetObjectAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(json!);
    }

    // Batch set objects
    public async Task SetObjectsAsync<T>(Dictionary<string, T> items, TimeSpan? expiry = null)
    {
        var batch = _db.CreateBatch();
        foreach (var kvp in items)
        {
            var json = JsonSerializer.Serialize(kvp.Value);
            batch.StringSetAsync(kvp.Key, json, expiry);
        }
        batch.Execute();
        await Task.CompletedTask;
    }

    // Batch get objects
    public async Task<Dictionary<string, T?>> GetObjectsAsync<T>(IEnumerable<string> keys)
    {
        var result = new Dictionary<string, T?>();
        var redisKeys = new List<RedisKey>();
        foreach (var key in keys)
            redisKeys.Add(key);

        var values = await _db.StringGetAsync(redisKeys.ToArray());
        for (int i = 0; i < redisKeys.Count; i++)
        {
            if (values[i].IsNullOrEmpty)
                result[redisKeys[i]] = default;
            else
                result[redisKeys[i]] = JsonSerializer.Deserialize<T>(values[i]!);
        }
        return result;
    }

    public async Task<bool> RemoveAsync(string key)
    {
        return await _db.KeyDeleteAsync(key);
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}

using System.Text.Json;
using OrbitalWatch.Api.Models;
using StackExchange.Redis;

namespace OrbitalWatch.Api.Services;

public class CurrentStateService(IConnectionMultiplexer mux, ILogger<CurrentStateService> logger)
{
    private readonly IDatabase _redis = mux.GetDatabase();

    // Key pattern: orbital:state:{satelliteId}
    private static string StateKey(int satelliteId) => $"orbital:state:{satelliteId}";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(10);

    public async Task<TelemetryEvent?> GetAsync(int satelliteId)
    {
        var key = StateKey(satelliteId);

        try
        {
            var cached = await _redis.StringGetAsync(key);
            if (cached.HasValue)
            {
                logger.LogDebug("Cache HIT for satellite {Id}.", satelliteId);
                return JsonSerializer.Deserialize<TelemetryEvent>(cached!.ToString());                
            }
        }
        catch (RedisException e)
        {   
            // Redis is unavailable
            logger.LogWarning(e, "Redis unavailble on GET for satellite {id}, Falling through to DB.", satelliteId);
        }

        return null; // cache miss - caller handles DB fallback
    }

    public async Task SetAsync(TelemetryEvent telemetryEvent)
    {
        var key = StateKey(telemetryEvent.SatelliteId);

        try
        {
            var json = JsonSerializer.Serialize(telemetryEvent);
            await _redis.StringSetAsync(key, json, Ttl);
            logger.LogDebug("Cache SET for satellite {Id} (TTL {Ttl}).", telemetryEvent.SatelliteId, Ttl);
        }
        catch (RedisException e)
        {
            logger.LogWarning(e, "Redis unavailable on SET for satellite {id}. Skipping cache.", telemetryEvent.SatelliteId);
        }
    }
}
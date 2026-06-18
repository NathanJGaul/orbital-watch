using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using OrbitalWatch.Api.Hubs;
using OrbitalWatch.Api.Models;
using StackExchange.Redis;

namespace OrbitalWatch.Api.Services;

// public class RedisSubscriberService(
//     IConnectionMultiplexer mux,
//     IHubContext<TelemetryHub> hubContext,
//     ILogger<RedisSubscriberService> logger): BackgroundService
// {
//     private readonly Dictionary<int, TelemetryEvent
// }

public class RedisSubscriberService(
    IConnectionMultiplexer mux,
    IHubContext<TelemetryHub> hubContext, // drive signalr hub from background service
    ILogger<RedisSubscriberService> logger) : BackgroundService
{
    // Conjunction detection state: last known position per satellite
    private readonly Dictionary<int, TelemetryEvent> _lastKnown = new(); // used with lock for atomic updates and reads

    // 200 km is a generous threshold for detecting conjunctions.
    // In the real world, conjunctions are much closer (around 5 km),
    // and we'd use the probability of collision to determine if a
    // conjunction is likely instead of the raw miss distance.
    private const double ConjunctionThresholdKm = 200.0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = mux.GetSubscriber();

        // Pattern subscribe: receives messages from ALl orbital:telemetry:*
        await subscriber.SubscribeAsync(RedisChannel.Pattern("orbital:telemetry:*"), OnTelemetryMessage);
        logger.LogInformation("RedisSubscriberService listening on orbital:telemetry:* channels.");

        // Keep the service alive until the app shuts down
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void OnTelemetryMessage(RedisChannel channel, RedisValue value)
    {
        // This callback is invoked on a Redis thread pool thread - keep it fast
        // Fire-and-forget the async fan-out to avoid blocking the Redis thread receive loop.
        _ = Task.Run(async () =>
        {
            try
            {
                await HandleMessageAsync(value);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing Redis message: {Message}", value);
            }
        });
    }

    private async Task HandleMessageAsync(RedisValue value)
    {
        var evt = JsonSerializer.Deserialize<TelemetryEvent>(value.ToString());
        if (evt is null) return;

        // Fan out to all clients subscribed to this satellite's group
        var groupName = TelemetryHub.GroupName(evt.SatelliteId);
        await hubContext.Clients.Group(groupName).SendAsync("TelemetryUpdate", evt);

        // Update last-known position and check for conjunection
        lock (_lastKnown)
        {
            _lastKnown[evt.SatelliteId] = evt;
        }

        await CheckConjunctionAsync(evt);
    }

    private async Task CheckConjunctionAsync(TelemetryEvent updated)
    {
        // Retrieve a snapshot of known satellites and their telemetry events
        List<TelemetryEvent> snapshot;
        lock (_lastKnown)
        {
            snapshot = _lastKnown.Values.ToList(); // creates a copy atomically
        }

        foreach (var other in snapshot)
        {
            if (other.SatelliteId == updated.SatelliteId) continue;
            if (updated.SatelliteId > other.SatelliteId) continue; // Prevent duplicate conjunction alerts
            var distKm = Distance3DKm(updated, other);
            if (distKm < ConjunctionThresholdKm)
            {
                logger.LogInformation("Conjunction detected: {Updated} and {Other} are {DistKm} km apart.", updated,
                    other, distKm);
                var alert = new ConjunctionAlert
                {
                    PrimarySatelliteId = updated.SatelliteId,
                    SecondarySatelliteId = other.SatelliteId,
                    MissDistanceKm = distKm,
                    DetectedAt = DateTime.UtcNow,
                    Severity = distKm < 50 ? "Critical" : distKm < 100 ? "High" : "Medium"
                };

                // Broadcast to all connected clients
                await hubContext.Clients.All.SendAsync("ConjunctionAlert", alert);
            }
        }
    }

    private static double Distance3DKm(TelemetryEvent a, TelemetryEvent b)
    {
        var (x1, y1, z1) = ToEcef(a);
        var (x2, y2, z2) = ToEcef(b);
        var dx = x2 - x1;
        var dy = y2 - y1;
        var dz = z2 - z1;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);

        static (double x, double y, double z) ToEcef(TelemetryEvent e)
        {
            const double a = 6378.137;
            const double eSq = 0.006694379;
            var lat = e.LatitudeDeg * Math.PI / 180.0;
            var lon = e.LongitudeDeg * Math.PI / 180.0;
            var sinLat = Math.Sin(lat);
            var cosLat = Math.Cos(lat);
            var n = a / Math.Sqrt(1.0 - eSq * sinLat * sinLat);
            var x = (n + e.AltitudeKm) * cosLat * Math.Cos(lon);
            var y = (n + e.AltitudeKm) * cosLat * Math.Sin(lon);
            var z = (n * (1.0 - eSq) + e.AltitudeKm) * sinLat;
            return (x, y, z);
        }
    }
}
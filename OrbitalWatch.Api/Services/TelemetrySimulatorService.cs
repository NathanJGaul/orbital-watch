using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrbitalWatch.Api.Data;
using OrbitalWatch.Api.Models;
using StackExchange.Redis;

namespace OrbitalWatch.Api.Services;

public class TelemetrySimulatorService(
    IServiceProvider services,
    IConnectionMultiplexer mux,
    ILogger<TelemetrySimulatorService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);

    private const double R = 6371.0; // Earth radius in km
    private const double GM = 398600.4418; //km^3/s^2
    private const double TwoPi = 2 * Math.PI;

    // Per-satellite orbital parameters - initialized once on startup
    private record OrbitalParams(
        double AltitudeKm,
        double InclinationDeg,
        double InitialLonDeg,
        double OrbitalPeriodSeconds
    );

    private readonly Dictionary<int, OrbitalParams> _orbits = new();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Wait for the app to finish starting before we begin ticking
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        await InitializeOrbitsAsync(ct);

        logger.LogInformation(
            "TelemetrySimulatorService started. Ticking every {Interval}s for {Count} satellites.",
            _interval.TotalSeconds, _orbits.Count
        );

        while (!ct.IsCancellationRequested)
        {
            var tickStart = DateTime.UtcNow;

            await TickAsync(tickStart, ct);

            // Sleep for the remainder of the interval to keep a steady 2s cadence
            var elapsed = DateTime.UtcNow - tickStart;
            var delay = _interval - elapsed;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct);
        }
    }

    private async Task InitializeOrbitsAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrbitalWatchDbContext>();

        var satellites = await db.Satellites
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var rng = new Random(42); // fixed seed so orbits are deterministic across restarts

        foreach (var sat in satellites)
        {
            double altKm = sat.OrbitalRegime switch
            {
                "LEO" => rng.NextDouble() * 800 + 400, // 400 - 1200 km
                "MEO" => rng.NextDouble() * 5000 + 8000, // 8000 - 13000 km
                "GEO" => 35786, // geostationary
                "HEO" => rng.NextDouble() * 30000 + 5000, // 5000 - 35000 km
                _ => 500
            };

            double periodSeconds = TwoPi *
                                   Math.Sqrt(Math.Pow(R + altKm, 3) / GM);

            _orbits[sat.Id] = new OrbitalParams(
                AltitudeKm: altKm,
                InclinationDeg: sat.OrbitalRegime == "GEO" ? 0 : rng.NextDouble() * 70 + 10,
                InitialLonDeg: rng.NextDouble() * 360 - 180,
                OrbitalPeriodSeconds: periodSeconds
            );
        }

        logger.LogInformation("Initialized orbital parameters for {Count} satellites.", _orbits.Count);
    }

    private async Task TickAsync(DateTime now, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrbitalWatchDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<CurrentStateService>();

        double tSeconds = (now - DateTime.UnixEpoch).TotalSeconds;
        var pub = mux.GetSubscriber();
        var events = new List<TelemetryEvent>();

        foreach (var (satId, orbit) in _orbits)
        {
            var (lat, lon, alt, vx, vy, vz, speed) = ComputeState(orbit, tSeconds);

            events.Add(new TelemetryEvent
            {
                SatelliteId = satId,
                Timestamp = now,
                LatitudeDeg = lat,
                LongitudeDeg = lon,
                AltitudeKm = alt,
                VelocityXKms = vx,
                VelocityYKms = vy,
                VelocityZKms = vz,
                SpeedKms = speed,
            });
        }

        db.TelemetryEvents.AddRange(events);
        await db.SaveChangesAsync(ct);

        // Publish each event to Redis and update the cache
        foreach (var evt in events)
        {
            var json = JsonSerializer.Serialize(evt);
            var channel = $"orbital:telemetry:{evt.SatelliteId}";

            try
            {
                await pub.PublishAsync(RedisChannel.Literal(channel), json);
                await cache.SetAsync(evt);
            }
            catch (RedisException e)
            {
                logger.LogError(e, "Redis unavailable on publish for satellite {id}. Skipping cache.", evt.SatelliteId);
            }
        }

        logger.LogDebug("Tick: wrote {Count} telemetry events at {TimeStamp}.", events.Count, now);
    }

    private static (double lat, double lon, double alt, double vx, double vy, double vz, double speed)
        ComputeState(OrbitalParams orbit, double tSeconds)
    {
        double n = TwoPi / orbit.OrbitalPeriodSeconds; // mean motion rad/s
        double lon = ((orbit.InitialLonDeg * Math.PI / 180) + n * tSeconds) % TwoPi;
        double lonDeg = lon * 180 / Math.PI;

        double incRad = orbit.InclinationDeg * Math.PI / 180;
        double latDeg = Math.Asin(Math.Sin(incRad) * Math.Sin(lon)); // sinusoidal ground track

        // Small altitude oscillation (+/- 10km) to simulate orbital eccentricity
        double altKm = orbit.AltitudeKm + 10 * Math.Sin(n * tSeconds * 3);

        // Velocity: tangential speed at this altitude
        double speedKms = Math.Sqrt(GM / (R + altKm)); // km/s

        // Approximate velocity components in ECI frame
        double vx = -speedKms * Math.Sin(lon);
        double vy = speedKms * Math.Cos(lon) * Math.Cos(incRad);
        double vz = speedKms * Math.Cos(lon) * Math.Sin(incRad);

        // Normalize longitude to [-180, 180]
        while (lonDeg > 180) lonDeg -= 360;
        while (lonDeg < -180) lonDeg += 360;

        return (latDeg, lonDeg, altKm, vx, vy, vz, speedKms);
    }
}
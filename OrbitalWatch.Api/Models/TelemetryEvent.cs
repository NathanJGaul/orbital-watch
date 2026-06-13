namespace OrbitalWatch.Api.Models;

public class TelemetryEvent
{
  public long Id { get; set; }
  public int SatelliteId { get; set; }
  public DateTime Timestamp { get; set; }

  // Position (geodetic)
  public double LatitudeDeg { get; set; }
  public double LongitudeDeg { get; set; }
  public double AltitudeKm { get; set; }

  // Velocity vector (km/s)
  public double VelocityXKms { get; set; }
  public double VelocityYKms { get; set; }
  public double VelocityZKms { get; set; }

  public double SpeedKms { get; set; } // pre-computed for quick queries

  public Satellite Satellite { get; set; } = null!;
}

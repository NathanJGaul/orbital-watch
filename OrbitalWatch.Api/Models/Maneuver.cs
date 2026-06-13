namespace OrbitalWatch.Api.Models;

public class Maneuver
{
  public int Id { get; set; }
  public int SatelliteId { get; set; }

  public DateTime PlannedAt { get; set; }
  public DateTime? ExecutedAt { get; set; }       // null until birn occurs

  public double DeltaVXKms { get; set; }
  public double DeltaVYKms { get; set; }
  public double DeltaVZKms { get; set; }
  public double DeltaVMagnitudeKms { get; set; }

  public string Purpose { get; set; } = string.Empty;   // "collision avoidance", "station-keeping"
  public string Status { get; set; } = "Planned";       // Planned | Executing | COmplete | Aborted
  
  public Satellite Satellite { get; set; } = null!;
}
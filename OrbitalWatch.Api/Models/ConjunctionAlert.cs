namespace OrbitalWatch.Api.Models;

public class ConjunctionAlert
{
  public int Id { get; set; }
  public int PrimarySatelliteId { get; set; }
  public int SecondarySatelliteId { get; set; }

  public DateTime DetectedAt { get; set; }
  public DateTime? TimeOfClosestApproach { get; set; }

  public double MissDistanceKm { get; set; }
  public double CollisionProbability { get; set; }

  public string Severity { get; set; } = "Low";
  public bool IsResolved { get; set; } = false;

  public Satellite PrimarySatellite { get; set; } = null!;
  public Satellite SecondarySatellite { get; set; } = null!;
}

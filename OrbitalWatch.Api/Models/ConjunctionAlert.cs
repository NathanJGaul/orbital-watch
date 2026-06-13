namespace OrbitalWatch.Api.Models;

public class ConjunctionAlert
{
  public int Id { get; set; }
  public int PrimarySatelliteId { get; set; }
  public int SecondarySatelliteId { get; set; }

  public DateTime DetectedAt { get; set; }
  public DateTime? TimeOfClosestApproach { get; set; }      // nullable: might not be computed yet

  public double MissDistanceKm { get; set; }
  public double CollisionProbability { get; set; }          // 0.0 - 1.0

  public string Severity { get; set; } = "Low";             // Low | Medium | High | Critical
  public bool IsResolved { get; set; } = false;

  public Satellite PrimarySatellite { get; set; } = null!;
  public Satellite SecondarySatellite { get; set; } = null!;
}

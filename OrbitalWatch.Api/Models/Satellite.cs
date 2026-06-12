namespace OrbitalWatch.Api.Models;

public class Satellite
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string NoradId { get; set; } = string.Empty; // e.g. "25544" for ISS
  public string OrbitalRegime { get; set; } = string.Empty; // LEO, MEO, GEO, HEO
  public string Owner { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public ICollection<TelemetryEvent> TelemetryEvents { get; set; } = new List<TelemetryEvent>();
  public ICollection<Maneuver> Maneuvers { get; set; } = new List<Maneuver>();
}

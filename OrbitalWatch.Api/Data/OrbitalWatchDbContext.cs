using Microsoft.EntityFrameworkCore;
using OrbitalWatch.Api.Models;

namespace OrbitalWatch.Api.Data;

public class OrbitalWatchDbContext(DbContextOptions<OrbitalWatchDbContext> options) : DbContext(options)
{
  public DbSet<Satellite> Satellites => Set<Satellite>();
  public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();
  public DbSet<ConjunctionAlert> ConjunctionAlerts => Set<ConjunctionAlert>();
  public DbSet<Maneuver> Maneuvers => Set<Maneuver>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // Satellite
    modelBuilder.Entity<Satellite>(e =>
      {
        e.HasIndex(s => s.NoradId).IsUnique();
        e.Property(s => s.OrbitalRegime).HasMaxLength(10);
      });

    // TelemetryEvent
    modelBuilder.Entity<TelemetryEvent>(e =>
      {
        e.HasIndex(t => new { t.SatelliteId, t.Timestamp }); // most queries filter by satellite id and time interval of event
        e.HasIndex(t => t.Timestamp); // global time internval queries

        e.HasOne(t => t.Satellite)
          .WithMany(s => s.TelemetryEvents)
          .HasForeignKey(t => t.SatelliteId)
          .OnDelete(DeleteBehavior.Cascade);
      });

    modelBuilder.Entity<ConjunctionAlert>(e =>
      {
        e.HasIndex(c => c.DetectedAt);
        e.HasIndex(c => new { c.PrimarySatelliteId, c.IsResolved });
        e.Property(c => c.Severity).HasMaxLength(20);

        e.HasOne(c => c.PrimarySatellite)
          .WithMany()
          .HasForeignKey(c => c.PrimarySatelliteId)
          .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(c => c.SecondarySatellite)
          .WithMany()
          .HasForeignKey(c => c.SecondarySatelliteId)
          .OnDelete(DeleteBehavior.Restrict);
      });

    modelBuilder.Entity<Maneuver>(e =>
      {
        e.HasIndex(m => new { m.SatelliteId, m.PlannedAt });

        e.HasOne(m => m.Satellite)
          .WithMany(s => s.Maneuvers)
          .HasForeignKey(m => m.SatelliteId)
          .OnDelete(DeleteBehavior.Cascade);
      });
  }
}

using Microsoft.EntityFrameworkCore;
using OrbitalWatch.Api.Models;
using OrbitalWatch.Api.Data;

namespace OrbitalWatch.Api.Repositories;

public class TelemetryRepository(OrbitalWatchDbContext db) : ITelemetryRepository
{
  public async Task<IEnumerable<TelemetryEvent>> GetRecentAsync(int satelliteId, int count = 20)
    => await db.TelemetryEvents
      .Where(t => t.SatelliteId == satelliteId)
      .OrderByDescending(t => t.Timestamp)
      .Take(count)
      .ToListAsync();

  public async Task<TelemetryEvent?> GetLatestAsync(int satelliteId)
    => await db.TelemetryEvents
      .Where(t => t.SatelliteId == satelliteId)
      .OrderByDescending(t => t.Timestamp)
      .FirstOrDefaultAsync();
}

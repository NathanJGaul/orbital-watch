using Microsoft.EntityFrameworkCore;
using OrbitalWatch.Api.Models;
using OrbitalWatch.Api.Data;

namespace OrbitalWatch.Api.Repositories;

public class SatelliteRepository(OrbitalWatchDbContext db) : ISatelliteRepository
{
  public async Task<IEnumerable<Satellite>> GetAllAsync()
    => await db.Satellites
        .Where(s => s.IsActive)
        .OrderBy(s => s.Name)
        .ToListAsync();

  public async Task<Satellite?> GetByIdAsync(int id)
    => await db.Satellites.FindAsync(id);

  public async Task<Satellite> AddAsync(Satellite satellite)
  {
    db.Satellites.Add(satellite);
    await db.SaveChangesAsync();
    return satellite;
  }
}

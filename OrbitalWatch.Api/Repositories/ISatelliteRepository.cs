using OrbitalWatch.Api.Models;

namespace OrbitalWatch.Api.Repositories;

public interface ISatelliteRepository
{
  Task<IEnumerable<Satellite>> GetAllAsync();
  Task<Satellite?> GetByIdAsync(int id);
  Task<Satellite> AddAsync(Satellite satellite);
}

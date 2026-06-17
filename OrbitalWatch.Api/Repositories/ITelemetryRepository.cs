using OrbitalWatch.Api.Models;

namespace OrbitalWatch.Api.Repositories;

public interface ITelemetryRepository
{
  Task<IEnumerable<TelemetryEvent>> GetRecentAsync(int satelliteId, int count = 20);
  Task<TelemetryEvent?> GetLatestAsync(int satelliteId);
}

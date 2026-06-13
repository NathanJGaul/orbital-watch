using Microsoft.EntityFrameworkCore;
using OrbitalWatch.Api.Models;

namespace OrbitalWatch.Api.Data;

public class SeedService(IServiceProvider services, ILogger<SeedService> logger) : IHostedService
{
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrbitalWatchDbContext>();

    if (await db.Satellites.AnyAsync(cancellationToken))
    {
      logger.LogInformation("Database already seeded, skipping.");
      return;
    }

    var satellites = new List<Satellite>
    {
      new() { Name = "STARLINK-1234", NoradId = "46123", OrbitalRegime = "LEO", Owner = "SpaceX" },
      new() { Name = "GPS-IIF-10",    NoradId = "40105", OrbitalRegime = "MEO", Owner = "USAF" },
      new() { Name = "INTELSAT-39",   NoradId = "44476", OrbitalRegime = "GEO", Owner = "Intelsat" },
      new() { Name = "COSMOS-2543",   NoradId = "45616", OrbitalRegime = "LEO", Owner = "Russia" },
      new() { Name = "ISS (ZARYA)",   NoradId = "25544", OrbitalRegime = "LEO", Owner = "ISS Program" },
    };

    db.Satellites.AddRange(satellites);
    await db.SaveChangesAsync(cancellationToken);
    logger.LogInformation("Seeded {Count} satellites.", satellites.Count);
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

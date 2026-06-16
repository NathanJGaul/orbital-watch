using Microsoft.AspNetCore.Mvc;
using OrbitalWatch.Api.Repositories;
using OrbitalWatch.Api.Services;

namespace OrbitalWatch.Api.Controllers;

[ApiController]
[Route("api/satellites/{satelliteId:int}/telemetry")]
public class TelemetryController(ITelemetryRepository repo, CurrentStateService cache) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetAll(int satelliteId, [FromQuery] int count = 20)
  {
    var events = await repo.GetRecentAsync(satelliteId, Math.Clamp(count, 1, 100));
    return Ok(events);
  }

  [HttpGet("latest")]
  public async Task<IActionResult> GetLatest(int satelliteId)
  {
    // 1. Try Redis cache
    var cached = await cache.GetAsync(satelliteId);
    if (cached is not null)
      return Ok(cached);
    
    // 2. Cache miss - read from DB
    var telemetryEvent = await repo.GetLatestAsync(satelliteId);
    if (telemetryEvent is null)
      return NotFound();
    
    // 3. Populate cache for next caller
    await cache.SetAsync(telemetryEvent);
    return Ok(telemetryEvent);
  }
}

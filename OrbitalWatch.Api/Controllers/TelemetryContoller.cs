using Microsoft.AspNetCore.Mvc;
using OrbitalWatch.Api.Repositories;

namespace OrbitalWatch.Api.Controllers;

[ApiController]
[Route("api/satellites/{satelliteId:int}/telemetry")]
public class TelemetryController(ITelemetryRepository repo) : ControllerBase
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
    var telemetryEvent = await repo.GetLatestAsync(satelliteId);
    return telemetryEvent is null ? NotFound() : Ok(telemetryEvent);
  }
}

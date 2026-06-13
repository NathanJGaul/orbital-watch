using Microsoft.AspNetCore.Mvc;
using OrbitalWatch.Api.Repositories;

namespace OrbitalWatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SatellitesController(ISatelliteRepository repo) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var satellites = await repo.GetAllAsync();
    return Ok(satellites);
  }

  [HttpGet("{id:int}")]
  public async Task<IActionResult> GetById(int id)
  {
    var satellite = await repo.GetByIdAsync(id);
    return satellite is null ? NotFound() : Ok(satellite);
  }
}

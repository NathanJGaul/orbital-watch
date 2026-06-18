using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrbitalWatch.Api.Services;

namespace OrbitalWatch.Api.Controllers;

/// <summary>
/// Dev-only authentication endpoint - no password or user lokup
/// </summary>
/// <param name="tokenService"></param>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController(TokenService tokenService) : ControllerBase
{
    public record LoginRequest(string UserId);

    [HttpPost("token")]
    public IActionResult GetToken([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest("UserId is required");

        var token = tokenService.GenerateToken(request.UserId);
        return Ok(new { token });
    }
}
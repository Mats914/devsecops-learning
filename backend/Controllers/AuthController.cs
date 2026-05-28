using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Services;

namespace DevSecOpsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    // POST /api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request, Ip);
        if (result is null)
            return Conflict(new { message = "Username is already taken." });
        return CreatedAtAction(nameof(Register), result);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request, Ip);
        if (result is null)
            return Unauthorized(new { message = "Invalid credentials." });
        return Ok(result);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, Ip);
        if (result is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        return Ok(result);
    }

    // POST /api/auth/revoke
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request)
    {
        await authService.RevokeAsync(request.RefreshToken);
        return NoContent();
    }

    // GET /api/auth/verify-email?token=...
    [HttpGet("verify-email")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var ok = await authService.VerifyEmailAsync(token);
        return ok ? Ok(new { message = "Email verified successfully." })
                  : BadRequest(new { message = "Invalid or expired verification token." });
    }
}

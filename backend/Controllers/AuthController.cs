using Microsoft.AspNetCore.Mvc;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Services;

namespace DevSecOpsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // POST /api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Model validation is done automatically via [ApiController]
        var result = await authService.RegisterAsync(request);

        if (result is null)
            return Conflict(new { message = "Username is already taken." });

        return CreatedAtAction(nameof(Register), result);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        if (result is null)
        {
            // Generic message: do NOT reveal whether username or password is wrong
            return Unauthorized(new { message = "Invalid credentials." });
        }

        return Ok(result);
    }
}

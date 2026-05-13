using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DevSecOpsApi.Data;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Models;

namespace DevSecOpsApi.Services;

// ── Interface ──────────────────────────────────────────────────────────────

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}

// ── Implementation ─────────────────────────────────────────────────────────

public class AuthService(AppDbContext db, IConfiguration config) : IAuthService
{
    private readonly string _key      = config["Jwt:Key"]!;
    private readonly string _issuer   = config["Jwt:Issuer"]!;
    private readonly string _audience = config["Jwt:Audience"]!;
    private readonly int    _expiry   = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Prevent duplicate usernames (case-insensitive)
        var exists = await db.Users
            .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());

        if (exists) return null;

        var user = new User
        {
            Username     = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = "User"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

        if (user is null) return null;

        // Verify password using BCrypt (timing-safe)
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return BuildAuthResponse(user);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private AuthResponse BuildAuthResponse(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiry);
        var token     = GenerateJwt(user, expiresAt);
        return new AuthResponse(token, user.Username, user.Role, expiresAt);
    }

    private string GenerateJwt(User user, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role)
        };

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

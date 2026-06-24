// AuthService.cs – hanterar registrering, inloggning, JWT och refresh tokens.
// BCrypt för lösenord, audit-loggning på viktiga händelser.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DevSecOpsApi.Data;
using DevSecOpsApi.DTOs;
using DevSecOpsApi.Models;

namespace DevSecOpsApi.Services;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ip);
    Task<AuthResponse?> LoginAsync(LoginRequest request, string? ip);
    Task<AuthResponse?> RefreshAsync(string refreshToken, string? ip);
    Task<bool>          RevokeAsync(string refreshToken);
    Task<bool>          VerifyEmailAsync(string token);
}

/// <summary>
/// All autentiseringslogik – registrering, login, token rotation m.m.
/// </summary>
public class AuthService(
    AppDbContext     db,
    IConfiguration   config,
    IEmailService    emailService,
    IAuditService    auditService) : IAuthService
{
    private readonly string _key       = config["Jwt:Key"]!;
    private readonly string _issuer    = config["Jwt:Issuer"]!;
    private readonly string _audience  = config["Jwt:Audience"]!;
    private readonly int    _accessExp = int.Parse(config["Jwt:AccessTokenExpiryMinutes"] ?? "15");
    private readonly int    _refreshExp= int.Parse(config["Jwt:RefreshTokenExpiryDays"]  ?? "7");

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest req, string? ip)
    {
        var exists = await db.Users
            .AnyAsync(u => u.Username.ToLower() == req.Username.ToLower());
        if (exists) return null;

        var verificationToken = GenerateSecureToken();
        var user = new User
        {
            Username          = req.Username,
            PasswordHash      = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Email             = req.Email,
            VerificationToken = verificationToken,
            EmailVerified     = req.Email is null   // ingen e-post = ingen verifiering behövs
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Skicka verifieringsmail i bakgrunden så vi inte blockerar svaret
        if (req.Email is not null)
            _ = emailService.SendVerificationEmailAsync(req.Email, req.Username, verificationToken);

        await auditService.LogAsync("REGISTER", $"User '{user.Username}' registered", ip, user.Id, true);

        var refresh = await CreateRefreshTokenAsync(user, ip);
        return BuildResponse(user, refresh.Token);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest req, string? ip)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == req.Username.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            await auditService.LogAsync("LOGIN_FAIL", $"Failed login for '{req.Username}'", ip, null, false);
            return null;
        }

        if (!user.IsActive)
        {
            await auditService.LogAsync("LOGIN_FAIL", $"Inactive account '{req.Username}'", ip, user.Id, false);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await auditService.LogAsync("LOGIN", $"User '{user.Username}' logged in", ip, user.Id, true);

        var refresh = await CreateRefreshTokenAsync(user, ip);
        return BuildResponse(user, refresh.Token);
    }

    public async Task<AuthResponse?> RefreshAsync(string token, string? ip)
    {
        var stored = await db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == token);

        if (stored is null || !stored.IsActive) return null;

        // Token rotation – det gamla ogiltigförklaras, nytt skapas
        stored.IsRevoked = true;
        var newRefresh = await CreateRefreshTokenAsync(stored.User, ip);

        await auditService.LogAsync("TOKEN_REFRESH", null, ip, stored.UserId, true);
        return BuildResponse(stored.User, newRefresh.Token);
    }

    public async Task<bool> RevokeAsync(string token)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (stored is null || !stored.IsActive) return false;
        stored.IsRevoked = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
        if (user is null) return false;
        user.EmailVerified     = true;
        user.VerificationToken = null;  // token ska bara gå att använda en gång
        await db.SaveChangesAsync();
        return true;
    }

    // ── Privata hjälpmetoder ───────────────────────────────────────────────

    private async Task<RefreshToken> CreateRefreshTokenAsync(User user, string? ip)
    {
        // Städa bort gamla/utgångna tokens så tabellen inte växer i all oändlighet
        var expired = db.RefreshTokens
            .Where(r => r.UserId == user.Id && (r.IsRevoked || r.ExpiresAt < DateTime.UtcNow));
        db.RefreshTokens.RemoveRange(expired);

        var rt = new RefreshToken
        {
            Token         = GenerateSecureToken(),
            ExpiresAt     = DateTime.UtcNow.AddDays(_refreshExp),
            UserId        = user.Id,
            CreatedByIp   = ip
        };
        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();
        return rt;
    }

    private AuthResponse BuildResponse(User user, string refreshToken)
    {
        var expiresAt  = DateTime.UtcNow.AddMinutes(_accessExp);
        var accessToken = GenerateJwt(user, expiresAt);
        return new AuthResponse(accessToken, refreshToken, user.Username,
                                user.Role, user.EmailVerified, expiresAt);
    }

    private string GenerateJwt(User user, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role)
        };
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt   = new JwtSecurityToken(_issuer, _audience, claims,
                                         expires: expiresAt, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    // URL-säker random sträng (base64 utan +/=)
    private static string GenerateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
               .Replace("+", "-").Replace("/", "_").Replace("=", "");
}

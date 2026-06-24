// User.cs – användarentitet med lösenordshash, roll och e-postverifiering.

using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.Models;

/// <summary>
/// Registrerad användare i systemet.
/// </summary>
public class User
{
    public int    Id            { get; set; }

    [Required, MaxLength(50)]
    public string Username      { get; set; } = string.Empty;

    [Required]
    public string PasswordHash  { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role          { get; set; } = "User";

    // E-postverifiering
    [MaxLength(200)]
    public string? Email        { get; set; }
    public bool   EmailVerified { get; set; } = false;
    public string? VerificationToken { get; set; }

    public bool   IsActive      { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigationsegenskaper – EF laddar relaterade poster
    public ICollection<Post>         Posts         { get; set; } = [];
    public ICollection<Comment>      Comments      { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<AuditLog>     AuditLogs     { get; set; } = [];
}

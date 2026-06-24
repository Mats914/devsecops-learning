// RefreshToken.cs – långlivad token för att hämta nya access tokens utan om-inloggning.
// Ogiltigförklaras vid rotation eller utgång.

using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.Models;

/// <summary>
/// Refresh token lagrad i databasen – kopplad till en användare.
/// </summary>
public class RefreshToken
{
    public int      Id         { get; set; }

    [Required, MaxLength(500)]
    public string   Token      { get; set; } = string.Empty;

    public DateTime ExpiresAt  { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public bool     IsRevoked  { get; set; } = false;

    [MaxLength(50)]
    public string?  CreatedByIp { get; set; }

    // Främmande nyckel till User
    public int  UserId { get; set; }
    public User User   { get; set; } = null!;

    public bool IsExpired  => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive   => !IsRevoked && !IsExpired;
}

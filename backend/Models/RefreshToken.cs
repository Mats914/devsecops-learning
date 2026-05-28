using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.Models;

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

    // FK
    public int  UserId { get; set; }
    public User User   { get; set; } = null!;

    public bool IsExpired  => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive   => !IsRevoked && !IsExpired;
}

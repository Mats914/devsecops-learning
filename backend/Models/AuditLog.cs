// AuditLog.cs – spårar säkerhetsrelaterade händelser (login, register, token refresh m.m.).

using System.ComponentModel.DataAnnotations;

namespace DevSecOpsApi.Models;

/// <summary>
/// En rad i audit-loggen – vem gjorde vad, när och om det lyckades.
/// </summary>
public class AuditLog
{
    public int    Id         { get; set; }

    [Required, MaxLength(100)]
    public string Action     { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Details   { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(300)]
    public string? UserAgent { get; set; }

    public bool   Success    { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int?   UserId    { get; set; }
    public User?  User      { get; set; }
}

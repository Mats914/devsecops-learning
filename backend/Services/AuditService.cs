// AuditService.cs – skriver säkerhetshändelser till audit_logs-tabellen.
// Anropas t.ex. vid lyckad/misslyckad inloggning och registrering.

using DevSecOpsApi.Data;
using DevSecOpsApi.Models;

namespace DevSecOpsApi.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? details, string? ip, int? userId, bool success);
}

/// <summary>
/// Enkel audit-loggning – sparar vem som gjorde vad och om det lyckades.
/// </summary>
public class AuditService(AppDbContext db) : IAuditService
{
    public async Task LogAsync(string action, string? details, string? ip, int? userId, bool success)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action    = action,
            Details   = details,
            IpAddress = ip,
            UserId    = userId,
            Success   = success
        });
        await db.SaveChangesAsync();
    }
}

// EmailService.cs – skickar verifierings- och återställningsmail via SMTP (MailKit).
// Kan stängas av i config så man slipper riktig mailserver under utveckling.

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DevSecOpsApi.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string username, string token);
    Task SendPasswordResetEmailAsync(string toEmail, string username, string token);
}

/// <summary>
/// E-postutskick – loggar bara om Email:Enabled är false.
/// </summary>
public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    private readonly bool _enabled = config.GetValue<bool>("Email:Enabled");

    public async Task SendVerificationEmailAsync(string toEmail, string username, string token)
    {
        var subject = "Verify your DevSecOps Demo account";
        var body    = $"""
            <h2>Welcome, {username}!</h2>
            <p>Click the link below to verify your email address:</p>
            <a href="http://localhost:5173/verify-email?token={token}">Verify Email</a>
            <p>This link expires in 24 hours.</p>
            """;
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string username, string token)
    {
        var subject = "Reset your DevSecOps Demo password";
        var body    = $"""
            <h2>Password Reset Request</h2>
            <p>Hi {username}, click below to reset your password:</p>
            <a href="http://localhost:5173/reset-password?token={token}">Reset Password</a>
            <p>This link expires in 1 hour. If you didn't request this, ignore this email.</p>
            """;
        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (!_enabled)
        {
            logger.LogInformation("Email disabled. Would send to {To}: {Subject}", to, subject);
            return;
        }

        try
        {
            var fromName    = config["Email:FromName"] ?? "DevSecOps Demo";
            var fromAddress = config["Email:FromAddress"];
            var smtpHost    = config["Email:SmtpHost"];
            var username    = config["Email:Username"];
            var password    = config["Email:Password"];

            // Saknas SMTP-uppgifter → avbryt tyst med varning i loggen
            if (string.IsNullOrWhiteSpace(fromAddress) ||
                string.IsNullOrWhiteSpace(smtpHost) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Email enabled but SMTP configuration is incomplete");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body    = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost,
                config.GetValue<int>("Email:SmtpPort"),
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Mail ska inte krascha hela appen om SMTP strular
            logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}

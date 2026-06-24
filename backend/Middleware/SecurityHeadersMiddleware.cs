// SecurityHeadersMiddleware.cs – lägger på säkerhetsheaders och loggar varje request.
// Körs tidigt i pipelinen (shift-left) innan controllerna.

namespace DevSecOpsApi.Middleware;

/// <summary>
/// Sätter HTTP-säkerhetsheaders på alla svar och loggar metod, path, status och tid.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var headers = ctx.Response.Headers;
        headers["X-Content-Type-Options"]    = "nosniff";
        headers["X-Frame-Options"]           = "DENY";
        headers["X-XSS-Protection"]          = "1; mode=block";
        headers["Referrer-Policy"]           = "strict-origin-when-cross-origin";
        headers["Content-Security-Policy"]   = "default-src 'none'; frame-ancestors 'none'";
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        headers["Permissions-Policy"]        = "camera=(), microphone=(), geolocation=()";
        headers.Remove("Server");  // dölj server-header så angripare får mindre info

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await next(ctx);
        sw.Stop();

        logger.LogInformation(
            "{Method} {Path} {StatusCode} {ElapsedMs}ms {IP}",
            ctx.Request.Method,
            ctx.Request.Path,
            ctx.Response.StatusCode,
            sw.ElapsedMilliseconds,
            ctx.Connection.RemoteIpAddress);
    }
}

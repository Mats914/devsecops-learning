namespace DevSecOpsApi.Middleware;

/// <summary>
/// Adds HTTP security headers to every response.
/// This is a "shift-left" control: applied as early as possible in the pipeline.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent browsers from guessing the content type
        headers["X-Content-Type-Options"] = "nosniff";

        // Disallow the page from being embedded in an iframe (clickjacking)
        headers["X-Frame-Options"] = "DENY";

        // Enable browser XSS filtering (legacy but harmless)
        headers["X-XSS-Protection"] = "1; mode=block";

        // Control what info is sent in the Referer header
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Strict Content Security Policy for an API (no HTML served)
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // HSTS – tell browsers to only use HTTPS (31536000 s = 1 year)
        // Only effective over HTTPS; harmless on HTTP
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Remove the server identification header
        headers.Remove("Server");

        await next(context);
    }
}

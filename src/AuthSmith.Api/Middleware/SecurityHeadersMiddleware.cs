namespace AuthSmith.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all HTTP responses.
/// Implements OWASP security best practices for headers.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Prevent MIME type sniffing
        context.Response.Headers.XContentTypeOptions = "nosniff";

        // X-Frame-Options: Prevent clickjacking
        context.Response.Headers.XFrameOptions = "DENY";

        // X-XSS-Protection: Enable XSS protection (legacy browsers)
        context.Response.Headers.XXSSProtection = "1; mode=block";

        // Referrer-Policy: Control referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content-Security-Policy: Prevent XSS and data injection attacks
        context.Response.Headers.ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // Swagger requires unsafe-eval
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        // Permissions-Policy: Control browser features
        context.Response.Headers["Permissions-Policy"] =
            "accelerometer=(), " +
            "camera=(), " +
            "geolocation=(), " +
            "gyroscope=(), " +
            "magnetometer=(), " +
            "microphone=(), " +
            "payment=(), " +
            "usb=()";

        // Strict-Transport-Security: Force HTTPS (only add if HTTPS)
        if (context.Request.IsHttps)
        {
            context.Response.Headers.StrictTransportSecurity =
                "max-age=31536000; includeSubDomains; preload";
        }

        // Remove server header for security through obscurity
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}

/// <summary>
/// Extension method to add security headers middleware to the pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

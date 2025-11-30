using System.Diagnostics;

namespace AuthSmith.Api.Middleware;

/// <summary>
/// Middleware for request/response logging with correlation IDs.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        using var activity = new Activity("Request");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? string.Empty;
            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            _logger.LogInformation("Request {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                method, path, statusCode, elapsedMs);
        }
    }
}


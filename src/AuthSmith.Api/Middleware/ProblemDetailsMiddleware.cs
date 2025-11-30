using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuthSmith.Contracts.Errors;

namespace AuthSmith.Api.Middleware;

/// <summary>
/// Middleware for formatting errors as RFC 7807 ProblemDetails.
/// </summary>
public class ProblemDetailsMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorType, title) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            ArgumentNullException => (HttpStatusCode.BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            ArgumentException => (HttpStatusCode.BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"),
            _ => (HttpStatusCode.InternalServerError, "https://tools.ietf.org/html/rfc7231#section-6.6.1", "Internal Server Error")
        };

        var problemDetails = new ErrorResponseDto
        {
            Type = errorType,
            Title = title,
            Status = (int)statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }
}


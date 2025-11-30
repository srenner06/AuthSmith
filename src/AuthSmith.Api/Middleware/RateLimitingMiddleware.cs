using System.Collections.Concurrent;
using System.Net;
using AuthSmith.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AuthSmith.Api.Middleware;

/// <summary>
/// Rate limiting middleware using sliding window algorithm.
/// Supports both in-memory and distributed (Redis) caching.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<RateLimitConfiguration> _config;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // In-memory tracking for local rate limiting
    private static readonly ConcurrentDictionary<string, SlidingWindow> _rateLimits = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<RateLimitConfiguration> config,
        IMemoryCache memoryCache,
        IDistributedCache? distributedCache,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _config = config;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var configuration = _config.Value;

        if (!configuration.Enabled)
        {
            await _next(context);
            return;
        }

        // Get client identifier (IP + optional API key)
        var clientId = GetClientIdentifier(context);
        var ipAddress = GetClientIpAddress(context);

        // Check if IP or API key is whitelisted
        if (IsWhitelisted(ipAddress, context, configuration))
        {
            await _next(context);
            return;
        }

        // Determine rate limit based on endpoint
        var (limit, window) = GetRateLimitForEndpoint(context.Request.Path, configuration);

        // Check rate limit
        var (allowed, remaining, resetTime) = await CheckRateLimitAsync(clientId, limit, window);

        // Add rate limit headers
        context.Response.Headers["X-Rate-Limit-Limit"] = limit.ToString();
        context.Response.Headers["X-Rate-Limit-Remaining"] = Math.Max(0, remaining).ToString();
        context.Response.Headers["X-Rate-Limit-Reset"] = resetTime.ToString("O");

        if (!allowed)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Path}",
                clientId, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = ((int)(resetTime - DateTime.UtcNow).TotalSeconds).ToString();

            await context.Response.WriteAsJsonAsync(new
            {
                error = "RateLimitExceeded",
                message = $"Rate limit exceeded. Maximum {limit} requests per {window} seconds allowed.",
                retryAfter = resetTime
            });
            return;
        }

        await _next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();

        // Combine IP and API key for unique client identification
        return !string.IsNullOrEmpty(apiKey)
            ? $"{ipAddress}:{apiKey[..8]}" // Use first 8 chars of API key
            : ipAddress;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsWhitelisted(string ipAddress, HttpContext context, RateLimitConfiguration config)
    {
        // Check IP whitelist
        if (config.WhitelistedIps.Contains(ipAddress))
        {
            return true;
        }

        // Check API key whitelist
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey) && config.WhitelistedApiKeys.Contains(apiKey))
        {
            return true;
        }

        return false;
    }

    private static (int Limit, int Window) GetRateLimitForEndpoint(PathString path, RateLimitConfiguration config)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        // Authentication endpoints (login, refresh)
        if (pathValue.Contains("/auth/login") || pathValue.Contains("/auth/refresh"))
        {
            return (config.AuthLimit, config.WindowSeconds);
        }

        // Registration endpoint
        if (pathValue.Contains("/auth/register"))
        {
            return (config.RegistrationLimit, 3600); // Per hour
        }

        // Password reset endpoint
        if (pathValue.Contains("/password-reset"))
        {
            return (config.PasswordResetLimit, 3600); // Per hour
        }

        // Default limit for all other endpoints
        return (config.GeneralLimit, config.WindowSeconds);
    }

    private async Task<(bool Allowed, int Remaining, DateTime ResetTime)> CheckRateLimitAsync(
        string clientId,
        int limit,
        int windowSeconds)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-windowSeconds);

        // Use distributed cache if available (for multi-instance deployments)
        if (_distributedCache != null && !string.IsNullOrEmpty(_config.Value.RedisConnectionString))
        {
            return await CheckDistributedRateLimitAsync(clientId, limit, windowSeconds, now, windowStart);
        }

        // Fall back to in-memory cache
        return CheckInMemoryRateLimit(clientId, limit, windowSeconds, now, windowStart);
    }

    private (bool Allowed, int Remaining, DateTime ResetTime) CheckInMemoryRateLimit(
        string clientId,
        int limit,
        int windowSeconds,
        DateTime now,
        DateTime windowStart)
    {
        var window = _rateLimits.GetOrAdd(clientId, _ => new SlidingWindow());

        lock (window)
        {
            // Remove expired timestamps
            window.Timestamps.RemoveAll(t => t < windowStart);

            var currentCount = window.Timestamps.Count;
            var remaining = limit - currentCount;
            var resetTime = window.Timestamps.Count > 0
                ? window.Timestamps[0].AddSeconds(windowSeconds)
                : now.AddSeconds(windowSeconds);

            if (currentCount >= limit)
            {
                return (false, remaining, resetTime);
            }

            // Add current request timestamp
            window.Timestamps.Add(now);
            return (true, remaining - 1, resetTime);
        }
    }

    private async Task<(bool Allowed, int Remaining, DateTime ResetTime)> CheckDistributedRateLimitAsync(
        string clientId,
        int limit,
        int windowSeconds,
        DateTime now,
        DateTime windowStart)
    {
        // Defensive check - should never be null due to caller check
        if (_distributedCache == null)
        {
            return CheckInMemoryRateLimit(clientId, limit, windowSeconds, now, windowStart);
        }

        // For distributed rate limiting, use Redis sorted sets with timestamps
        // This is a simplified implementation - consider using a library like StackExchange.Redis.RateLimiting
        var key = $"rate_limit:{clientId}";

        try
        {
            var cachedData = await _distributedCache.GetStringAsync(key);
            var timestamps = string.IsNullOrEmpty(cachedData)
                ? new List<DateTime>()
                : System.Text.Json.JsonSerializer.Deserialize<List<DateTime>>(cachedData) ?? new List<DateTime>();

            // Remove expired timestamps
            timestamps.RemoveAll(t => t < windowStart);

            var currentCount = timestamps.Count;
            var remaining = limit - currentCount;
            var resetTime = timestamps.Count > 0
                ? timestamps[0].AddSeconds(windowSeconds)
                : now.AddSeconds(windowSeconds);

            if (currentCount >= limit)
            {
                return (false, remaining, resetTime);
            }

            // Add current timestamp
            timestamps.Add(now);
            var serialized = System.Text.Json.JsonSerializer.Serialize(timestamps);
            await _distributedCache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(windowSeconds)
            });

            return (true, remaining - 1, resetTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking distributed rate limit for {ClientId}, falling back to allow", clientId);
            // On error, allow the request (fail open)
            return (true, 0, now.AddSeconds(windowSeconds));
        }
    }

    private class SlidingWindow
    {
        public List<DateTime> Timestamps { get; } = new();
    }
}

/// <summary>
/// Extension methods for rate limiting middleware.
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}

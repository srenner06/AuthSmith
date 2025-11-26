using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AuthSmith.Infrastructure.Services.Caching;

/// <summary>
/// In-memory permission cache implementation (fallback when Redis is unavailable).
/// </summary>
public partial class InMemoryPermissionCache : IPermissionCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryPermissionCache> _logger;
    private const int CacheExpirationMinutes = 15;

    public InMemoryPermissionCache(IMemoryCache memoryCache, ILogger<InMemoryPermissionCache> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, applicationId);
        var cached = _memoryCache.Get<HashSet<string>>(key);
        return Task.FromResult(cached);
    }

    public Task SetUserPermissionsAsync(Guid userId, Guid applicationId, HashSet<string> permissions, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, applicationId);
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
        };
        _memoryCache.Set(key, permissions, options);
        return Task.CompletedTask;
    }

    public Task InvalidateUserPermissionsAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, applicationId);
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // In-memory cache doesn't support pattern-based invalidation easily
        // This is a limitation - Redis implementation will be better
        LogInvalidatingUserPermissions(_logger, userId);
        return Task.CompletedTask;
    }

    public Task InvalidateApplicationPermissionsAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        // In-memory cache doesn't support pattern-based invalidation easily
        LogInvalidatingApplicationPermissions(_logger, applicationId);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Invalidating all permissions for user {UserId} - in-memory cache has limited support")]
    private static partial void LogInvalidatingUserPermissions(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Invalidating all permissions for application {ApplicationId} - in-memory cache has limited support")]
    private static partial void LogInvalidatingApplicationPermissions(ILogger logger, Guid applicationId);

    private static string GetCacheKey(Guid userId, Guid applicationId)
    {
        return $"permissions:user:{userId}:app:{applicationId}";
    }
}


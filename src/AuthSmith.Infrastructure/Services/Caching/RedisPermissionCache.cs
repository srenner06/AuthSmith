using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuthSmith.Infrastructure.Services.Caching;

/// <summary>
/// Redis-based permission cache implementation.
/// </summary>
public class RedisPermissionCache : IPermissionCache
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisPermissionCache> _logger;
    private const int CacheExpirationMinutes = 15;

    public RedisPermissionCache(IConnectionMultiplexer redis, ILogger<RedisPermissionCache> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, applicationId);
        var cached = await _database.StringGetAsync(key);

        if (!cached.HasValue)
            return null;

        try
        {
            var permissions = JsonSerializer.Deserialize<HashSet<string>>(cached.ToString()!);
            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize cached permissions for user {UserId} and application {ApplicationId}", userId, applicationId);
            return null;
        }
    }

    public async Task SetUserPermissionsAsync(Guid userId, Guid applicationId, HashSet<string> permissions, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, applicationId);
        var serialized = JsonSerializer.Serialize(permissions);
        await _database.StringSetAsync(key, serialized, TimeSpan.FromMinutes(CacheExpirationMinutes));
    }

    public async Task InvalidateUserPermissionsAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, applicationId);
        await _database.KeyDeleteAsync(key);
    }

    public async Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pattern = $"permissions:user:{userId}:app:*";
        var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern).ToArray();

        if (keys.Length > 0)
        {
            await _database.KeyDeleteAsync(keys);
            _logger.LogDebug("Invalidated {Count} permission cache entries for user {UserId}", keys.Length, userId);
        }
    }

    public async Task InvalidateApplicationPermissionsAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var pattern = $"permissions:user:*:app:{applicationId}";
        var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern).ToArray();

        if (keys.Length > 0)
        {
            await _database.KeyDeleteAsync(keys);
            _logger.LogDebug("Invalidated {Count} permission cache entries for application {ApplicationId}", keys.Length, applicationId);
        }
    }

    private static string GetCacheKey(Guid userId, Guid applicationId)
    {
        return $"permissions:user:{userId}:app:{applicationId}";
    }
}


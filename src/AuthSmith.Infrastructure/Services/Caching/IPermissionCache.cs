namespace AuthSmith.Infrastructure.Services.Caching;

/// <summary>
/// Cache service for permission lookups to improve performance.
/// </summary>
public interface IPermissionCache
{
    /// <summary>
    /// Gets cached permissions for a user and application.
    /// </summary>
    Task<HashSet<string>?> GetUserPermissionsAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches permissions for a user and application.
    /// </summary>
    Task SetUserPermissionsAsync(Guid userId, Guid applicationId, HashSet<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached permissions for a user and application.
    /// </summary>
    Task InvalidateUserPermissionsAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached permissions for a user.
    /// </summary>
    Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached permissions for an application.
    /// </summary>
    Task InvalidateApplicationPermissionsAsync(Guid applicationId, CancellationToken cancellationToken = default);
}

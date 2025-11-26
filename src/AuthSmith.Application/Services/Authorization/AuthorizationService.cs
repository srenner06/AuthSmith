using AuthSmith.Contracts.Authorization;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;

namespace AuthSmith.Application.Services.Authorization;

/// <summary>
/// Service for checking user permissions and authorization.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if a user has a specific permission for a module and action in an application.
    /// </summary>
    Task<OneOf<PermissionCheckResultDto, NotFoundError>> CheckPermissionAsync(PermissionCheckRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk permission checks for multiple users and permissions in a single operation.
    /// </summary>
    Task<OneOf<BulkPermissionCheckResultDto, NotFoundError>> BulkCheckPermissionsAsync(BulkPermissionCheckRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all permissions for a user in an application, optionally filtered by module.
    /// </summary>
    Task<OneOf<HashSet<string>, NotFoundError>> GetUserPermissionsAsync(Guid userId, string applicationKey, string? moduleName = null, CancellationToken cancellationToken = default);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPermissionCache _permissionCache;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        AuthSmithDbContext dbContext,
        IPermissionCache permissionCache,
        ILogger<AuthorizationService> logger)
    {
        _dbContext = dbContext;
        _permissionCache = permissionCache;
        _logger = logger;
    }

    public async Task<OneOf<PermissionCheckResultDto, NotFoundError>> CheckPermissionAsync(PermissionCheckRequestDto request, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Key == request.ApplicationKey && a.IsActive, cancellationToken);
        if (application == null)
            return new NotFoundError($"Application '{request.ApplicationKey}' not found or inactive.");

        var permissionCode = $"{request.ApplicationKey}.{request.Module}.{request.Action}".ToLowerInvariant();
        var cachedPermissions = await _permissionCache.GetUserPermissionsAsync(request.UserId, application.Id, cancellationToken);
        if (cachedPermissions != null)
        {
            var hasPermission = cachedPermissions.Contains(permissionCode);
            return new PermissionCheckResultDto
            {
                HasPermission = hasPermission,
                Source = hasPermission ? "Cache" : "None"
            };
        }

        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Code == permissionCode && p.ApplicationId == application.Id, cancellationToken);

        if (permission == null)
        {
            return new PermissionCheckResultDto
            {
                HasPermission = false,
                Source = "None"
            };
        }

        var hasViaRole = await _dbContext.UserRoles
            .Where(ur => ur.UserId == request.UserId)
            .Join(_dbContext.RolePermissions.Where(rp => rp.PermissionId == permission.Id),
                ur => ur.RoleId,
                rp => rp.RoleId,
                (ur, rp) => rp)
            .AnyAsync(cancellationToken);

        if (hasViaRole)
        {
            await GetUserPermissionsInternalAsync(request.UserId, application.Id, cancellationToken);

            return new PermissionCheckResultDto
            {
                HasPermission = true,
                Source = "Role"
            };
        }

        var hasDirect = await _dbContext.UserPermissions
            .AnyAsync(up => up.UserId == request.UserId && up.PermissionId == permission.Id, cancellationToken);

        if (hasDirect)
        {
            await GetUserPermissionsInternalAsync(request.UserId, application.Id, cancellationToken);

            return new PermissionCheckResultDto
            {
                HasPermission = true,
                Source = "Direct"
            };
        }

        return new PermissionCheckResultDto
        {
            HasPermission = false,
            Source = "None"
        };
    }

    public async Task<OneOf<BulkPermissionCheckResultDto, NotFoundError>> BulkCheckPermissionsAsync(BulkPermissionCheckRequestDto request, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Key == request.ApplicationKey && a.IsActive, cancellationToken);
        if (application == null)
            return new NotFoundError($"Application '{request.ApplicationKey}' not found or inactive.");

        var allPermissions = await GetUserPermissionsInternalAsync(request.UserId, application.Id, cancellationToken);

        var results = new List<PermissionCheckResultItemDto>();

        foreach (var check in request.Checks)
        {
            var permissionCode = $"{request.ApplicationKey}.{check.Module}.{check.Action}".ToLowerInvariant();
            var hasPermission = allPermissions.Contains(permissionCode);

            results.Add(new PermissionCheckResultItemDto
            {
                Module = check.Module,
                Action = check.Action,
                HasPermission = hasPermission
            });
        }

        return new BulkPermissionCheckResultDto
        {
            Results = results
        };
    }

    public async Task<OneOf<HashSet<string>, NotFoundError>> GetUserPermissionsAsync(Guid userId, string applicationKey, string? moduleName = null, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Key == applicationKey && a.IsActive, cancellationToken);
        if (application == null)
            return new NotFoundError($"Application '{applicationKey}' not found or inactive.");
        var allPermissions = await GetUserPermissionsInternalAsync(userId, application.Id, cancellationToken);

        if (moduleName != null)
        {
            var modulePrefix = $"{applicationKey}.{moduleName}.".ToLowerInvariant();
            var filtered = allPermissions.Where(p => p.StartsWith(modulePrefix, StringComparison.OrdinalIgnoreCase)).ToHashSet();
            return (OneOf<HashSet<string>, NotFoundError>)filtered;
        }

        return (OneOf<HashSet<string>, NotFoundError>)allPermissions;
    }

    private async Task<HashSet<string>> GetUserPermissionsInternalAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken)
    {
        var cached = await _permissionCache.GetUserPermissionsAsync(userId, applicationId, cancellationToken);
        if (cached != null)
            return cached;

        var roleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_dbContext.Roles.Where(r => r.ApplicationId == applicationId),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Id)
            .ToListAsync(cancellationToken);

        var permissionsFromRoles = await _dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_dbContext.Permissions,
                rp => rp.PermissionId,
                p => p.Id,
                (rp, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var directPermissions = await _dbContext.UserPermissions
            .Where(up => up.UserId == userId)
            .Join(_dbContext.Permissions.Where(p => p.ApplicationId == applicationId),
                up => up.PermissionId,
                p => p.Id,
                (up, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allPermissions = permissionsFromRoles.Union(directPermissions).ToHashSet();
        await _permissionCache.SetUserPermissionsAsync(userId, applicationId, allPermissions, cancellationToken);

        return allPermissions;
    }
}

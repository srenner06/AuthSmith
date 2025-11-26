using AuthSmith.Contracts.Users;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;

namespace AuthSmith.Application.Services.Users;

/// <summary>
/// Service for managing users and their role/permission assignments.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    Task<OneOf<UserDto, NotFoundError>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches users by username or email. Returns empty list if query is null or empty.
    /// </summary>
    Task<List<UserDto>> SearchAsync(string? query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user. Returns conflict if user already has the role.
    /// </summary>
    Task<OneOf<Success, NotFoundError, ConflictError>> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task<OneOf<Success, NotFoundError>> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a direct permission to a user. Returns conflict if user already has the permission.
    /// </summary>
    Task<OneOf<Success, NotFoundError, ConflictError>> AssignPermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a direct permission from a user.
    /// </summary>
    Task<OneOf<Success, NotFoundError>> RemovePermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all role and permission assignments for a user in a specific application.
    /// </summary>
    Task<OneOf<Success, NotFoundError>> RevokeAllAssignmentsAsync(Guid userId, Guid appId, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPermissionCache _permissionCache;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AuthSmithDbContext dbContext,
        IPermissionCache permissionCache,
        ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _permissionCache = permissionCache;
        _logger = logger;
    }

    public async Task<OneOf<UserDto, NotFoundError>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return new NotFoundError { Message = "User not found." };

        return MapToDto(user);
    }

    public async Task<List<UserDto>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var usersQuery = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.ToUpperInvariant();
            usersQuery = usersQuery.Where(u =>
                u.NormalizedUserName.Contains(normalizedQuery) ||
                u.NormalizedEmail.Contains(normalizedQuery));
        }

        var users = await usersQuery
            .OrderBy(u => u.UserName)
            .Take(100)
            .ToListAsync(cancellationToken);

        return [.. users.Select(MapToDto)];
    }

    public async Task<OneOf<Success, NotFoundError, ConflictError>> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user == null)
            return new NotFoundError { Message = "User not found." };

        var role = await _dbContext.Roles.FindAsync([roleId], cancellationToken);
        if (role == null)
            return new NotFoundError { Message = "Role not found." };

        var existing = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (existing != null)
            return new ConflictError("User already has this role.");

        _dbContext.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = roleId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _permissionCache.InvalidateUserPermissionsAsync(userId, role.ApplicationId, cancellationToken);

        return Success.Instance;
    }

    public async Task<OneOf<Success, NotFoundError>> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (userRole == null)
            return new NotFoundError { Message = "User role not found." };

        var role = await _dbContext.Roles.FindAsync([roleId], cancellationToken);

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (role != null)
            await _permissionCache.InvalidateUserPermissionsAsync(userId, role.ApplicationId, cancellationToken);

        return Success.Instance;
    }

    public async Task<OneOf<Success, NotFoundError, ConflictError>> AssignPermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user == null)
            return new NotFoundError { Message = "User not found." };

        var permission = await _dbContext.Permissions.FindAsync([permissionId], cancellationToken);
        if (permission == null)
            return new NotFoundError { Message = "Permission not found." };

        var existing = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId, cancellationToken);

        if (existing != null)
            return new ConflictError("User already has this permission.");

        _dbContext.UserPermissions.Add(new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _permissionCache.InvalidateUserPermissionsAsync(userId, permission.ApplicationId, cancellationToken);

        return Success.Instance;
    }

    public async Task<OneOf<Success, NotFoundError>> RemovePermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var userPermission = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId, cancellationToken);
        if (userPermission == null)
            return new NotFoundError { Message = "User permission not found." };

        var permission = await _dbContext.Permissions.FindAsync([permissionId], cancellationToken);

        _dbContext.UserPermissions.Remove(userPermission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (permission != null)
            await _permissionCache.InvalidateUserPermissionsAsync(userId, permission.ApplicationId, cancellationToken);

        return Success.Instance;
    }

    public async Task<OneOf<Success, NotFoundError>> RevokeAllAssignmentsAsync(Guid userId, Guid appId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user == null)
            return new NotFoundError { Message = "User not found." };

        var application = await _dbContext.Applications.FindAsync([appId], cancellationToken);
        if (application == null)
            return new NotFoundError { Message = "Application not found." };

        var roleIds = await _dbContext.Roles
            .Where(r => r.ApplicationId == appId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var userRoles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ToListAsync(cancellationToken);

        _dbContext.UserRoles.RemoveRange(userRoles);

        var permissionIds = await _dbContext.Permissions
            .Where(p => p.ApplicationId == appId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var userPermissions = await _dbContext.UserPermissions
            .Where(up => up.UserId == userId && permissionIds.Contains(up.PermissionId))
            .ToListAsync(cancellationToken);

        _dbContext.UserPermissions.RemoveRange(userPermissions);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _permissionCache.InvalidateUserPermissionsAsync(userId, appId, cancellationToken);

        return Success.Instance;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }
}


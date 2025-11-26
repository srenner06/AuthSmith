using AuthSmith.Contracts.Roles;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;

namespace AuthSmith.Application.Services.Roles;

/// <summary>
/// Service for managing roles and their permission assignments within applications.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Creates a new role for an application. Returns conflict if role name already exists.
    /// </summary>
    Task<OneOf<RoleDto, NotFoundError, ConflictError>> CreateAsync(Guid appId, CreateRoleRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all roles for an application.
    /// </summary>
    Task<OneOf<List<RoleDto>, NotFoundError>> ListAsync(Guid appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a role by its unique identifier within an application.
    /// </summary>
    Task<OneOf<RoleDto, NotFoundError>> GetByIdAsync(Guid appId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a permission to a role. Returns conflict if permission is already assigned.
    /// </summary>
    Task<OneOf<Success, NotFoundError, ConflictError>> AssignPermissionAsync(Guid appId, Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a permission from a role.
    /// </summary>
    Task<OneOf<Success, NotFoundError>> RemovePermissionAsync(Guid appId, Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role. This will remove the role from all users who have it assigned.
    /// </summary>
    Task<OneOf<Success, NotFoundError>> DeleteAsync(Guid appId, Guid roleId, CancellationToken cancellationToken = default);
}

public class RoleService : IRoleService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPermissionCache _permissionCache;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        AuthSmithDbContext dbContext,
        IPermissionCache permissionCache,
        ILogger<RoleService> logger)
    {
        _dbContext = dbContext;
        _permissionCache = permissionCache;
        _logger = logger;
    }

    public async Task<OneOf<RoleDto, NotFoundError, ConflictError>> CreateAsync(Guid appId, CreateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == appId, cancellationToken);
        if (application == null)
            return new NotFoundError { Message = "Application not found." };

        var existing = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.ApplicationId == appId && r.Name == request.Name, cancellationToken);

        if (existing != null)
            return new ConflictError($"Role '{request.Name}' already exists for this application.");

        var role = new Role
        {
            ApplicationId = appId,
            Name = request.Name,
            Description = request.Description
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(role);
    }

    public async Task<OneOf<List<RoleDto>, NotFoundError>> ListAsync(Guid appId, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == appId, cancellationToken);
        if (application == null)
            return new NotFoundError { Message = "Application not found." };

        var roles = await _dbContext.Roles
            .Where(r => r.ApplicationId == appId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var roleDtos = roles.Select(MapToDto).ToList();
        return (OneOf<List<RoleDto>, NotFoundError>)roleDtos;
    }

    public async Task<OneOf<RoleDto, NotFoundError>> GetByIdAsync(Guid appId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.ApplicationId == appId, cancellationToken);

        if (role == null)
            return new NotFoundError { Message = "Role not found." };

        return MapToDto(role);
    }

    public async Task<OneOf<Success, NotFoundError, ConflictError>> AssignPermissionAsync(Guid appId, Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles.FindAsync([roleId], cancellationToken);
        if (role == null || role.ApplicationId != appId)
            return new NotFoundError { Message = "Role not found." };

        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Id == permissionId && p.ApplicationId == appId, cancellationToken);
        if (permission == null)
            return new NotFoundError { Message = "Permission not found or does not belong to this application." };

        var existing = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

        if (existing != null)
            return new ConflictError("Role already has this permission.");

        _dbContext.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var userIds = await _dbContext.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await _permissionCache.InvalidateUserPermissionsAsync(userId, appId, cancellationToken);
        }

        return Success.Instance;
    }

    public async Task<OneOf<Success, NotFoundError>> RemovePermissionAsync(Guid appId, Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermission = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
        if (rolePermission == null)
            return new NotFoundError { Message = "Role permission not found." };

        _dbContext.RolePermissions.Remove(rolePermission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userIds = await _dbContext.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await _permissionCache.InvalidateUserPermissionsAsync(userId, appId, cancellationToken);
        }
        return Success.Instance;
    }

    public async Task<OneOf<Success, NotFoundError>> DeleteAsync(Guid appId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.ApplicationId == appId, cancellationToken);
        if (role == null)
            return new NotFoundError { Message = "Role not found." };
        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var userIds = await _dbContext.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await _permissionCache.InvalidateUserPermissionsAsync(userId, appId, cancellationToken);
        }
        return Success.Instance;
    }

    private static RoleDto MapToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            ApplicationId = role.ApplicationId,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt
        };
    }
}


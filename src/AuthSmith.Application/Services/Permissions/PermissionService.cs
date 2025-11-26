using AuthSmith.Contracts.Permissions;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;

namespace AuthSmith.Application.Services.Permissions;

/// <summary>
/// Service for managing permissions within applications.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Creates a new permission for an application. Permission code is auto-generated from application key, module, and action.
    /// </summary>
    Task<OneOf<PermissionDto, NotFoundError, ConflictError>> CreateAsync(Guid appId, CreatePermissionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all permissions for an application.
    /// </summary>
    Task<OneOf<List<PermissionDto>, NotFoundError>> ListAsync(Guid appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a permission by its unique identifier within an application.
    /// </summary>
    Task<OneOf<PermissionDto, NotFoundError>> GetByIdAsync(Guid appId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a permission. This will remove the permission from all roles and users who have it assigned.
    /// </summary>
    Task<OneOf<Success, NotFoundError>> DeleteAsync(Guid appId, Guid permissionId, CancellationToken cancellationToken = default);
}

public class PermissionService : IPermissionService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPermissionCache _permissionCache;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        AuthSmithDbContext dbContext,
        IPermissionCache permissionCache,
        ILogger<PermissionService> logger)
    {
        _dbContext = dbContext;
        _permissionCache = permissionCache;
        _logger = logger;
    }

    public async Task<OneOf<PermissionDto, NotFoundError, ConflictError>> CreateAsync(Guid appId, CreatePermissionRequestDto request, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == appId, cancellationToken);
        if (application == null)
            return new NotFoundError { Message = "Application not found." };

        var permissionCode = $"{application.Key}.{request.Module}.{request.Action}".ToLowerInvariant();
        var existing = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.ApplicationId == appId && p.Code == permissionCode, cancellationToken);

        if (existing != null)
            return new ConflictError($"Permission with code '{permissionCode}' already exists for this application.");

        var permission = new Permission
        {
            ApplicationId = appId,
            Module = request.Module,
            Action = request.Action,
            Code = permissionCode,
            Description = request.Description
        };

        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(permission);
    }

    public async Task<OneOf<List<PermissionDto>, NotFoundError>> ListAsync(Guid appId, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == appId, cancellationToken);
        if (application == null)
            return new NotFoundError { Message = "Application not found." };

        var permissions = await _dbContext.Permissions
            .Where(p => p.ApplicationId == appId)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);

        var permissionDtos = permissions.Select(MapToDto).ToList();
        return (OneOf<List<PermissionDto>, NotFoundError>)permissionDtos;
    }

    public async Task<OneOf<PermissionDto, NotFoundError>> GetByIdAsync(Guid appId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Id == permissionId && p.ApplicationId == appId, cancellationToken);

        if (permission == null)
            return new NotFoundError { Message = "Permission not found." };

        return MapToDto(permission);
    }

    public async Task<OneOf<Success, NotFoundError>> DeleteAsync(Guid appId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Id == permissionId && p.ApplicationId == appId, cancellationToken);
        if (permission == null)
            return new NotFoundError { Message = "Permission not found." };
        _dbContext.Permissions.Remove(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _permissionCache.InvalidateApplicationPermissionsAsync(appId, cancellationToken);
        return Success.Instance;
    }

    private static PermissionDto MapToDto(Permission permission)
    {
        return new PermissionDto
        {
            Id = permission.Id,
            ApplicationId = permission.ApplicationId,
            Module = permission.Module,
            Action = permission.Action,
            Code = permission.Code,
            Description = permission.Description,
            CreatedAt = permission.CreatedAt
        };
    }
}


using AuthSmith.Contracts.Roles;
using Refit;

namespace AuthSmith.Sdk.Roles;

public interface IRolesClient
{
    [Post("/api/v1/apps/{appId}/roles")]
    Task<RoleDto> CreateAsync(Guid appId, [Body] CreateRoleRequestDto request);

    [Get("/api/v1/apps/{appId}/roles")]
    Task<List<RoleDto>> ListAsync(Guid appId);

    [Get("/api/v1/apps/{appId}/roles/{roleId}")]
    Task<RoleDto> GetByIdAsync(Guid appId, Guid roleId);

    [Post("/api/v1/apps/{appId}/roles/{roleId}/permissions")]
    Task AssignPermissionsAsync(Guid appId, Guid roleId, [Body] AssignPermissionRequestDto request);

    [Delete("/api/v1/apps/{appId}/roles/{roleId}/permissions/{permissionId}")]
    Task RemovePermissionAsync(Guid appId, Guid roleId, Guid permissionId);
}


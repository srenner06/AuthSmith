using AuthSmith.Contracts.Permissions;
using Refit;

namespace AuthSmith.Sdk.Permissions;

public interface IPermissionsClient
{
    [Post("/api/v1/apps/{appId}/permissions")]
    Task<PermissionDto> CreateAsync(Guid appId, [Body] CreatePermissionRequestDto request);

    [Get("/api/v1/apps/{appId}/permissions")]
    Task<List<PermissionDto>> ListAsync(Guid appId);

    [Get("/api/v1/apps/{appId}/permissions/{permissionId}")]
    Task<PermissionDto> GetByIdAsync(Guid appId, Guid permissionId);

    [Delete("/api/v1/apps/{appId}/permissions/{permissionId}")]
    Task DeleteAsync(Guid appId, Guid permissionId);
}


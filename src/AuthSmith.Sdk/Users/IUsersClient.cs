using AuthSmith.Contracts.Users;
using Refit;

namespace AuthSmith.Sdk.Users;

public interface IUsersClient
{
    [Get("/api/v1/users/{userId}")]
    Task<UserDto> GetByIdAsync(Guid userId);

    [Get("/api/v1/users")]
    Task<List<UserDto>> SearchAsync([Query] string? query = null);

    [Get("/api/v1/users/{userId}/permissions")]
    Task<List<string>> GetPermissionsAsync(Guid userId, [Query] string appKey, [Query] string? moduleName = null);

    [Post("/api/v1/users/{userId}/roles")]
    Task AssignRoleAsync(Guid userId, [Body] AssignRoleRequestDto request);

    [Delete("/api/v1/users/{userId}/roles/{roleId}")]
    Task RemoveRoleAsync(Guid userId, Guid roleId);

    [Post("/api/v1/users/{userId}/permissions")]
    Task AssignPermissionAsync(Guid userId, [Body] AssignPermissionRequestDto request);

    [Delete("/api/v1/users/{userId}/permissions/{permissionId}")]
    Task RemovePermissionAsync(Guid userId, Guid permissionId);

    [Delete("/api/v1/users/{userId}/apps/{appId}/assignments")]
    Task RevokeAllAssignmentsAsync(Guid userId, Guid appId);
}


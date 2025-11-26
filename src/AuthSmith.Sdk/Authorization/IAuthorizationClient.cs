using AuthSmith.Contracts.Authorization;
using Refit;

namespace AuthSmith.Sdk.Authorization;

public interface IAuthorizationClient
{
    [Post("/api/v1/authorization/check")]
    Task<PermissionCheckResultDto> CheckAsync([Body] PermissionCheckRequestDto request);

    [Post("/api/v1/authorization/bulk-check")]
    Task<BulkPermissionCheckResultDto> BulkCheckAsync([Body] BulkPermissionCheckRequestDto request);
}


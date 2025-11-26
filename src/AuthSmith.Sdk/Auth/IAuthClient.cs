using AuthSmith.Contracts.Auth;
using Refit;

namespace AuthSmith.Sdk.Auth;

public interface IAuthClient
{
    [Post("/api/v1/auth/register/{appKey}")]
    Task<AuthResultDto> RegisterAsync(string appKey, [Body] RegisterRequestDto request);

    [Post("/api/v1/auth/login")]
    Task<AuthResultDto> LoginAsync([Body] LoginRequestDto request);

    [Post("/api/v1/auth/refresh")]
    Task<AuthResultDto> RefreshAsync([Body] RefreshRequestDto request);

    [Post("/api/v1/auth/revoke")]
    Task RevokeAsync([Body] RevokeRefreshTokenRequestDto request);
}


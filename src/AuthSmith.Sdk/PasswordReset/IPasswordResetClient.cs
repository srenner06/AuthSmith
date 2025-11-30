using AuthSmith.Contracts.Auth;
using Refit;

namespace AuthSmith.Sdk.PasswordReset;

/// <summary>
/// Client for password reset endpoints.
/// </summary>
public interface IPasswordResetClient
{
    /// <summary>
    /// Request a password reset token.
    /// </summary>
    [Post("/api/v1/password-reset/request")]
    Task<PasswordResetResponseDto> RequestPasswordResetAsync(
        [Body] PasswordResetRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset password using token.
    /// </summary>
    [Post("/api/v1/password-reset/reset")]
    Task ResetPasswordAsync(
        [Body] PasswordResetConfirmDto request,
        CancellationToken cancellationToken = default);
}

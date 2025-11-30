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
    /// Get password reset form page (GET request for browser links).
    /// Returns an HTML page with instructions for resetting password.
    /// </summary>
    [Get("/api/v1/password-reset/confirm")]
    Task<string> GetPasswordResetFormAsync(
        [Query] string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset password using token (POST request).
    /// </summary>
    [Post("/api/v1/password-reset/confirm")]
    Task<PasswordResetResponseDto> ResetPasswordAsync(
        [Body] PasswordResetConfirmDto request,
        CancellationToken cancellationToken = default);
}

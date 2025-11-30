using AuthSmith.Contracts.Auth;
using Refit;

namespace AuthSmith.Sdk.EmailVerification;

/// <summary>
/// Client for email verification endpoints.
/// </summary>
public interface IEmailVerificationClient
{
    /// <summary>
    /// Verify email address using token.
    /// </summary>
    [Post("/api/v1/email-verification/verify")]
    Task<EmailVerificationResponseDto> VerifyEmailAsync(
        [Body] VerifyEmailDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend verification email.
    /// </summary>
    [Post("/api/v1/email-verification/resend")]
    Task ResendVerificationEmailAsync(
        [Body] ResendVerificationEmailDto request,
        CancellationToken cancellationToken = default);
}

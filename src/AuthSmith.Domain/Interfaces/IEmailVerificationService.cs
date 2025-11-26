namespace AuthSmith.Domain.Interfaces;

/// <summary>
/// Interface for email verification service.
/// This is a placeholder interface with no implementation in v1.
/// Reserved for future use when email integration is added.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Sends a verification email to the user.
    /// </summary>
    /// <param name="email">The email address to send verification to.</param>
    /// <param name="verificationToken">The verification token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendVerificationEmailAsync(string email, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an email address using a verification token.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="verificationToken">The verification token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if verification is successful, false otherwise.</returns>
    Task<bool> VerifyEmailAsync(string email, string verificationToken, CancellationToken cancellationToken = default);
}


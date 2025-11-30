namespace AuthSmith.Infrastructure.Services.Email;

/// <summary>
/// Email service interface for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a plain text email.
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an HTML email.
    /// </summary>
    Task<bool> SendHtmlEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send welcome email after registration.
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string to, string username, string appName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email verification email.
    /// </summary>
    Task<bool> SendEmailVerificationAsync(string to, string username, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send password reset email.
    /// </summary>
    Task<bool> SendPasswordResetEmailAsync(string to, string username, string resetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send password changed notification.
    /// </summary>
    Task<bool> SendPasswordChangedEmailAsync(string to, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send account locked notification.
    /// </summary>
    Task<bool> SendAccountLockedEmailAsync(string to, string username, DateTime lockedUntil, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send two-factor authentication code.
    /// </summary>
    Task<bool> SendTwoFactorCodeEmailAsync(string to, string username, string code, CancellationToken cancellationToken = default);
}

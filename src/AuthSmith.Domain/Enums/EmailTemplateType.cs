namespace AuthSmith.Domain.Entities;

/// <summary>
/// Email template types for different email scenarios.
/// </summary>
public enum EmailTemplateType
{
    /// <summary>
    /// Welcome email after registration.
    /// </summary>
    Welcome,

    /// <summary>
    /// Email verification.
    /// </summary>
    EmailVerification,

    /// <summary>
    /// Password reset request.
    /// </summary>
    PasswordReset,

    /// <summary>
    /// Password changed notification.
    /// </summary>
    PasswordChanged,

    /// <summary>
    /// Account locked notification.
    /// </summary>
    AccountLocked,

    /// <summary>
    /// Two-factor authentication code.
    /// </summary>
    TwoFactorCode
}

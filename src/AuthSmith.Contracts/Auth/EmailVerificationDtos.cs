namespace AuthSmith.Contracts.Auth;

/// <summary>
/// Request to resend email verification.
/// </summary>
public class ResendVerificationEmailDto
{
    /// <summary>
    /// Email address of the user.
    /// </summary>
    public required string Email { get; init; }
}

/// <summary>
/// Request to verify email with token.
/// </summary>
public class VerifyEmailDto
{
    /// <summary>
    /// Email verification token from email.
    /// </summary>
    public required string Token { get; init; }
}

/// <summary>
/// Response for email verification operations.
/// </summary>
public class EmailVerificationResponseDto
{
    /// <summary>
    /// Message indicating the result.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Whether the email was successfully verified.
    /// </summary>
    public bool IsVerified { get; init; }
}

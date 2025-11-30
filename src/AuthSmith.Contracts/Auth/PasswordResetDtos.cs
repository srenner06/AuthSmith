namespace AuthSmith.Contracts.Auth;

/// <summary>
/// Request to initiate password reset.
/// </summary>
public class PasswordResetRequestDto
{
    /// <summary>
    /// Email address of the user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Application key.
    /// </summary>
    public required string ApplicationKey { get; init; }
}

/// <summary>
/// Request to confirm password reset with token.
/// </summary>
public class PasswordResetConfirmDto
{
    /// <summary>
    /// Password reset token from email.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// New password.
    /// </summary>
    public required string NewPassword { get; init; }
}

/// <summary>
/// Response for password reset request.
/// </summary>
public class PasswordResetResponseDto
{
    /// <summary>
    /// Message indicating the request was processed.
    /// </summary>
    public required string Message { get; init; }
}

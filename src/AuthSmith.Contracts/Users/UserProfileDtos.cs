namespace AuthSmith.Contracts.Users;

/// <summary>
/// User profile information.
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public bool EmailVerified { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Request to update user profile.
/// </summary>
public class UpdateProfileDto
{
    /// <summary>
    /// New username (optional).
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// New email (optional, requires verification).
    /// </summary>
    public string? Email { get; init; }
}

/// <summary>
/// Request to change password.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// Current password.
    /// </summary>
    public required string CurrentPassword { get; init; }

    /// <summary>
    /// New password.
    /// </summary>
    public required string NewPassword { get; init; }
}

/// <summary>
/// Request to delete user account.
/// </summary>
public class DeleteAccountDto
{
    /// <summary>
    /// Password confirmation for account deletion.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Confirmation text (must be "DELETE").
    /// </summary>
    public required string Confirmation { get; init; }
}

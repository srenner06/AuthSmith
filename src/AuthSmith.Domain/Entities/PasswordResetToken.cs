namespace AuthSmith.Domain.Entities;

/// <summary>
/// Password reset token for secure password reset flow.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public string? IpAddress { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;

    public bool IsValid() => !IsUsed && DateTimeOffset.UtcNow < ExpiresAt;
}

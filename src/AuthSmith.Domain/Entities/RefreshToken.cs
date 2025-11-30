namespace AuthSmith.Domain.Entities;

/// <summary>
/// Represents a refresh token for user authentication.
/// Supports revocation for security.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Application Application { get; set; } = null!;
}


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
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Application Application { get; set; } = null!;
}


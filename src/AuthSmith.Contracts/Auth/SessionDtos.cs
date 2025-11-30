namespace AuthSmith.Contracts.Auth;

/// <summary>
/// Active session information.
/// </summary>
public class UserSessionDto
{
    public Guid Id { get; init; }
    public required string DeviceInfo { get; init; }
    public string? IpAddress { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public bool IsCurrentSession { get; init; }
}

/// <summary>
/// List of user sessions.
/// </summary>
public class UserSessionsDto
{
    public required List<UserSessionDto> Sessions { get; init; }
    public int TotalCount { get; init; }
}

/// <summary>
/// Request to revoke a specific session.
/// </summary>
public class RevokeSessionDto
{
    /// <summary>
    /// Session ID (refresh token ID) to revoke.
    /// </summary>
    public Guid SessionId { get; init; }
}

/// <summary>
/// Request to revoke all sessions except current.
/// </summary>
public class RevokeAllSessionsDto
{
    /// <summary>
    /// Password confirmation.
    /// </summary>
    public required string Password { get; init; }
}

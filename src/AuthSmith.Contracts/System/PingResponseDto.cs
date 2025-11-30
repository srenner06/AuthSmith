namespace AuthSmith.Contracts.System;

/// <summary>
/// Response model for ping endpoint.
/// </summary>
public class PingResponseDto
{
    /// <summary>
    /// Version tag (e.g., v1.0.0).
    /// </summary>
    public string VersionTag { get; set; } = string.Empty;

    /// <summary>
    /// CI/CD build number.
    /// </summary>
    public string BuildNumber { get; set; } = string.Empty;

    /// <summary>
    /// Build timestamp in ISO 8601 format (UTC).
    /// </summary>
    public string BuildTime { get; set; } = string.Empty;

    /// <summary>
    /// Git commit hash (short form).
    /// </summary>
    public string CommitHash { get; set; } = string.Empty;

    /// <summary>
    /// Status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the request is authenticated.
    /// </summary>
    public bool Authenticated { get; set; }
}

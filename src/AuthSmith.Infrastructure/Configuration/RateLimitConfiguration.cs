namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// Rate limiting configuration.
/// </summary>
public class RateLimitConfiguration
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// Enable or disable rate limiting globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// General API rate limit (requests per minute).
    /// </summary>
    public int GeneralLimit { get; set; } = 100;

    /// <summary>
    /// Authentication endpoint rate limit (requests per minute).
    /// Higher risk endpoints get lower limits.
    /// </summary>
    public int AuthLimit { get; set; } = 10;

    /// <summary>
    /// Registration endpoint rate limit (requests per hour).
    /// </summary>
    public int RegistrationLimit { get; set; } = 5;

    /// <summary>
    /// Password reset endpoint rate limit (requests per hour).
    /// </summary>
    public int PasswordResetLimit { get; set; } = 3;

    /// <summary>
    /// Time window in seconds for rate limits.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Redis connection string for distributed rate limiting.
    /// If empty, uses in-memory cache (single instance only).
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// IP addresses to whitelist (bypass rate limiting).
    /// </summary>
    public string[] WhitelistedIps { get; set; } = [];

    /// <summary>
    /// API keys to whitelist (bypass rate limiting).
    /// </summary>
    public string[] WhitelistedApiKeys { get; set; } = [];
}

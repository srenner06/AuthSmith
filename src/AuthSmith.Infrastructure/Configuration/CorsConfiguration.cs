namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// CORS configuration for cross-origin requests.
/// </summary>
public class CorsConfiguration
{
    public const string SectionName = "Cors";

    /// <summary>
    /// List of allowed origins. Use "*" to allow all origins (not recommended for production).
    /// </summary>
    public string[] AllowedOrigins { get; set; } = ["http://localhost:3000", "http://localhost:5173"];

    /// <summary>
    /// Allow credentials (cookies, authorization headers) in CORS requests.
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Maximum age for CORS preflight cache in seconds.
    /// </summary>
    public int MaxAge { get; set; } = 3600;

    /// <summary>
    /// Additional allowed headers beyond default.
    /// </summary>
    public string[] AllowedHeaders { get; set; } = ["X-API-Key", "X-Request-Id"];

    /// <summary>
    /// Headers exposed to the client.
    /// </summary>
    public string[] ExposedHeaders { get; set; } = ["X-Request-Id", "X-Rate-Limit-Remaining"];
}

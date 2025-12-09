namespace AuthSmith.Infrastructure;

/// <summary>
/// Build and version information injected at build time.
/// Placeholders are replaced by CI/CD pipeline during the build process.
/// </summary>
public static class VersionInfo
{
    /// <summary>
    /// CI/CD build number.
    /// </summary>
    public const string BuildNumber = "__BUILD_NUMBER__";

    /// <summary>
    /// Build timestamp in ISO 8601 format (UTC).
    /// </summary>
    public const string BuildTime = "__BUILD_TIME__";

    /// <summary>
    /// Version tag (e.g., v1.0.0).
    /// </summary>
    public const string VersionTag = "__VERSION_TAG__";

    /// <summary>
    /// Git commit hash (short form).
    /// </summary>
    public const string CommitHash = "__COMMIT_HASH__";

    /// <summary>
    /// Gets a formatted version string suitable for logging and telemetry.
    /// Returns the version tag if available, otherwise the build number.
    /// </summary>
    public static string GetVersion()
    {
        // If running in development without CI/CD replacement
        if (VersionTag.StartsWith("__", StringComparison.Ordinal) && BuildNumber.StartsWith("__", StringComparison.Ordinal))
        {
            return "dev-local";
        }

        // Prefer version tag if available
        if (!VersionTag.StartsWith("__", StringComparison.Ordinal))
        {
            return VersionTag;
        }

        // Fall back to build number
        if (!BuildNumber.StartsWith("__", StringComparison.Ordinal))
        {
            return $"build-{BuildNumber}";
        }

        return "unknown";
    }

    /// <summary>
    /// Gets a short version identifier (e.g., "v1.0.0" or "build-123").
    /// </summary>
    public static string ShortVersion => GetVersion();

    /// <summary>
    /// Gets a full version string with build metadata.
    /// </summary>
    public static string FullVersion
    {
        get
        {
            var version = GetVersion();
            var commit = CommitHash.StartsWith("__", StringComparison.Ordinal) ? "local" : CommitHash;
            return $"{version} ({commit})";
        }
    }
}

namespace AuthSmith.Api;

/// <summary>
/// Build and version information injected at build time.
/// Placeholders are replaced by CI/CD pipeline.
/// </summary>
public static class Version
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
}

namespace AuthSmith.Api.Constants;

/// <summary>
/// Constants for authorization policies and related values.
/// </summary>
public static class AuthorizationConstants
{
    /// <summary>
    /// Policy name for requiring App-level or Admin-level API key access.
    /// </summary>
    public const string RequireAppAccessPolicy = "RequireAppAccess";

    /// <summary>
    /// Policy name for requiring Admin-level API key access.
    /// </summary>
    public const string RequireAdminAccessPolicy = "RequireAdminAccess";
}


namespace AuthSmith.Contracts.Enums;

/// <summary>
/// Defines the self-registration mode for an application.
/// </summary>
public enum SelfRegistrationMode
{
    /// <summary>
    /// Self-registration is disabled. Users cannot register themselves.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Self-registration is open. Anyone may register.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Self-registration is invite-only. Reserved for future use.
    /// </summary>
    InviteOnly = 2
}

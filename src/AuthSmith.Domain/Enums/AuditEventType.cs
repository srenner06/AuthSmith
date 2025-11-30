namespace AuthSmith.Domain.Enums;

/// <summary>
/// Types of audit events.
/// </summary>
public enum AuditEventType
{
    // Authentication events
    UserRegistered,
    UserLoggedIn,
    UserLoggedOut,
    LoginFailed,
    RefreshTokenUsed,
    RefreshTokenRevoked,

    // Account management
    PasswordChanged,
    PasswordResetRequested,
    PasswordResetCompleted,
    EmailVerificationSent,
    EmailVerified,
    ProfileUpdated,
    AccountDeleted,
    AccountLocked,
    AccountUnlocked,

    // Session management
    SessionCreated,
    SessionRevoked,
    AllSessionsRevoked,

    // Authorization
    PermissionGranted,
    PermissionRevoked,
    RoleAssigned,
    RoleRemoved,

    // Application management
    ApplicationCreated,
    ApplicationUpdated,
    ApplicationDeleted,
    ApiKeyGenerated,
    ApiKeyRevoked,

    // Security events
    SuspiciousActivity,
    BruteForceDetected,
    UnauthorizedAccess
}

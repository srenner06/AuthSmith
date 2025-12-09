using ContractEnums = AuthSmith.Contracts.Enums;
using DomainEnums = AuthSmith.Domain.Enums;

namespace AuthSmith.Application.Mapping;

/// <summary>
/// Extension methods for mapping between Contract and Domain enums.
/// </summary>
public static class EnumMappingExtensions
{
    /// <summary>
    /// Maps Contract SelfRegistrationMode to Domain SelfRegistrationMode.
    /// </summary>
    public static DomainEnums.SelfRegistrationMode ToDomain(this ContractEnums.SelfRegistrationMode mode)
    {
        return mode switch
        {
            ContractEnums.SelfRegistrationMode.Disabled => DomainEnums.SelfRegistrationMode.Disabled,
            ContractEnums.SelfRegistrationMode.Open => DomainEnums.SelfRegistrationMode.Open,
            ContractEnums.SelfRegistrationMode.InviteOnly => DomainEnums.SelfRegistrationMode.InviteOnly,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown SelfRegistrationMode value")
        };
    }

    /// <summary>
    /// Maps Domain SelfRegistrationMode to Contract SelfRegistrationMode.
    /// </summary>
    public static ContractEnums.SelfRegistrationMode ToContract(this DomainEnums.SelfRegistrationMode mode)
    {
        return mode switch
        {
            DomainEnums.SelfRegistrationMode.Disabled => ContractEnums.SelfRegistrationMode.Disabled,
            DomainEnums.SelfRegistrationMode.Open => ContractEnums.SelfRegistrationMode.Open,
            DomainEnums.SelfRegistrationMode.InviteOnly => ContractEnums.SelfRegistrationMode.InviteOnly,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown SelfRegistrationMode value")
        };
    }

    /// <summary>
    /// Maps Contract AuditEventType to Domain AuditEventType.
    /// </summary>
    public static DomainEnums.AuditEventType ToDomain(this ContractEnums.AuditEventType eventType)
    {
        return eventType switch
        {
            ContractEnums.AuditEventType.UserRegistered => DomainEnums.AuditEventType.UserRegistered,
            ContractEnums.AuditEventType.UserLoggedIn => DomainEnums.AuditEventType.UserLoggedIn,
            ContractEnums.AuditEventType.UserLoggedOut => DomainEnums.AuditEventType.UserLoggedOut,
            ContractEnums.AuditEventType.LoginFailed => DomainEnums.AuditEventType.LoginFailed,
            ContractEnums.AuditEventType.RefreshTokenUsed => DomainEnums.AuditEventType.RefreshTokenUsed,
            ContractEnums.AuditEventType.RefreshTokenRevoked => DomainEnums.AuditEventType.RefreshTokenRevoked,
            ContractEnums.AuditEventType.PasswordChanged => DomainEnums.AuditEventType.PasswordChanged,
            ContractEnums.AuditEventType.PasswordResetRequested => DomainEnums.AuditEventType.PasswordResetRequested,
            ContractEnums.AuditEventType.PasswordResetCompleted => DomainEnums.AuditEventType.PasswordResetCompleted,
            ContractEnums.AuditEventType.EmailVerificationSent => DomainEnums.AuditEventType.EmailVerificationSent,
            ContractEnums.AuditEventType.EmailVerified => DomainEnums.AuditEventType.EmailVerified,
            ContractEnums.AuditEventType.ProfileUpdated => DomainEnums.AuditEventType.ProfileUpdated,
            ContractEnums.AuditEventType.AccountDeleted => DomainEnums.AuditEventType.AccountDeleted,
            ContractEnums.AuditEventType.AccountLocked => DomainEnums.AuditEventType.AccountLocked,
            ContractEnums.AuditEventType.AccountUnlocked => DomainEnums.AuditEventType.AccountUnlocked,
            ContractEnums.AuditEventType.SessionCreated => DomainEnums.AuditEventType.SessionCreated,
            ContractEnums.AuditEventType.SessionRevoked => DomainEnums.AuditEventType.SessionRevoked,
            ContractEnums.AuditEventType.AllSessionsRevoked => DomainEnums.AuditEventType.AllSessionsRevoked,
            ContractEnums.AuditEventType.PermissionGranted => DomainEnums.AuditEventType.PermissionGranted,
            ContractEnums.AuditEventType.PermissionRevoked => DomainEnums.AuditEventType.PermissionRevoked,
            ContractEnums.AuditEventType.RoleAssigned => DomainEnums.AuditEventType.RoleAssigned,
            ContractEnums.AuditEventType.RoleRemoved => DomainEnums.AuditEventType.RoleRemoved,
            ContractEnums.AuditEventType.ApplicationCreated => DomainEnums.AuditEventType.ApplicationCreated,
            ContractEnums.AuditEventType.ApplicationUpdated => DomainEnums.AuditEventType.ApplicationUpdated,
            ContractEnums.AuditEventType.ApplicationDeleted => DomainEnums.AuditEventType.ApplicationDeleted,
            ContractEnums.AuditEventType.ApiKeyGenerated => DomainEnums.AuditEventType.ApiKeyGenerated,
            ContractEnums.AuditEventType.ApiKeyRevoked => DomainEnums.AuditEventType.ApiKeyRevoked,
            ContractEnums.AuditEventType.SuspiciousActivity => DomainEnums.AuditEventType.SuspiciousActivity,
            ContractEnums.AuditEventType.BruteForceDetected => DomainEnums.AuditEventType.BruteForceDetected,
            ContractEnums.AuditEventType.UnauthorizedAccess => DomainEnums.AuditEventType.UnauthorizedAccess,
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Unknown AuditEventType value")
        };
    }

    /// <summary>
    /// Maps Domain AuditEventType to Contract AuditEventType.
    /// </summary>
    public static ContractEnums.AuditEventType ToContract(this DomainEnums.AuditEventType eventType)
    {
        return eventType switch
        {
            DomainEnums.AuditEventType.UserRegistered => ContractEnums.AuditEventType.UserRegistered,
            DomainEnums.AuditEventType.UserLoggedIn => ContractEnums.AuditEventType.UserLoggedIn,
            DomainEnums.AuditEventType.UserLoggedOut => ContractEnums.AuditEventType.UserLoggedOut,
            DomainEnums.AuditEventType.LoginFailed => ContractEnums.AuditEventType.LoginFailed,
            DomainEnums.AuditEventType.RefreshTokenUsed => ContractEnums.AuditEventType.RefreshTokenUsed,
            DomainEnums.AuditEventType.RefreshTokenRevoked => ContractEnums.AuditEventType.RefreshTokenRevoked,
            DomainEnums.AuditEventType.PasswordChanged => ContractEnums.AuditEventType.PasswordChanged,
            DomainEnums.AuditEventType.PasswordResetRequested => ContractEnums.AuditEventType.PasswordResetRequested,
            DomainEnums.AuditEventType.PasswordResetCompleted => ContractEnums.AuditEventType.PasswordResetCompleted,
            DomainEnums.AuditEventType.EmailVerificationSent => ContractEnums.AuditEventType.EmailVerificationSent,
            DomainEnums.AuditEventType.EmailVerified => ContractEnums.AuditEventType.EmailVerified,
            DomainEnums.AuditEventType.ProfileUpdated => ContractEnums.AuditEventType.ProfileUpdated,
            DomainEnums.AuditEventType.AccountDeleted => ContractEnums.AuditEventType.AccountDeleted,
            DomainEnums.AuditEventType.AccountLocked => ContractEnums.AuditEventType.AccountLocked,
            DomainEnums.AuditEventType.AccountUnlocked => ContractEnums.AuditEventType.AccountUnlocked,
            DomainEnums.AuditEventType.SessionCreated => ContractEnums.AuditEventType.SessionCreated,
            DomainEnums.AuditEventType.SessionRevoked => ContractEnums.AuditEventType.SessionRevoked,
            DomainEnums.AuditEventType.AllSessionsRevoked => ContractEnums.AuditEventType.AllSessionsRevoked,
            DomainEnums.AuditEventType.PermissionGranted => ContractEnums.AuditEventType.PermissionGranted,
            DomainEnums.AuditEventType.PermissionRevoked => ContractEnums.AuditEventType.PermissionRevoked,
            DomainEnums.AuditEventType.RoleAssigned => ContractEnums.AuditEventType.RoleAssigned,
            DomainEnums.AuditEventType.RoleRemoved => ContractEnums.AuditEventType.RoleRemoved,
            DomainEnums.AuditEventType.ApplicationCreated => ContractEnums.AuditEventType.ApplicationCreated,
            DomainEnums.AuditEventType.ApplicationUpdated => ContractEnums.AuditEventType.ApplicationUpdated,
            DomainEnums.AuditEventType.ApplicationDeleted => ContractEnums.AuditEventType.ApplicationDeleted,
            DomainEnums.AuditEventType.ApiKeyGenerated => ContractEnums.AuditEventType.ApiKeyGenerated,
            DomainEnums.AuditEventType.ApiKeyRevoked => ContractEnums.AuditEventType.ApiKeyRevoked,
            DomainEnums.AuditEventType.SuspiciousActivity => ContractEnums.AuditEventType.SuspiciousActivity,
            DomainEnums.AuditEventType.BruteForceDetected => ContractEnums.AuditEventType.BruteForceDetected,
            DomainEnums.AuditEventType.UnauthorizedAccess => ContractEnums.AuditEventType.UnauthorizedAccess,
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Unknown AuditEventType value")
        };
    }
}

namespace AuthSmith.Domain.Entities;

/// <summary>
/// Represents a user in the system. Users are global across all applications.
/// </summary>
public class User : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Account lockout fields
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}


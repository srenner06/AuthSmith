using AuthSmith.Domain.Enums;

namespace AuthSmith.Domain.Entities;

/// <summary>
/// Represents a client application (e.g., ShopFlow, MiniScore, AC Tracker).
/// </summary>
public class Application : BaseEntity
{
    public string Key { get; set; } = string.Empty; // e.g., "shopflow"
    public string Name { get; set; } = string.Empty;
    public SelfRegistrationMode SelfRegistrationMode { get; set; } = SelfRegistrationMode.Disabled;
    public Guid? DefaultRoleId { get; set; }
    public bool IsActive { get; set; } = true;

    // API key for this application (hashed)
    public string? ApiKeyHash { get; set; }

    // Account lockout configuration (per application)
    public bool AccountLockoutEnabled { get; set; } = true;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;

    // Navigation properties
    public Role? DefaultRole { get; set; }
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<Permission> Permissions { get; set; } = [];
}


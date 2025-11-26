using AuthSmith.Domain.Enums;

namespace AuthSmith.Contracts.Applications;

public class ApplicationDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SelfRegistrationMode SelfRegistrationMode { get; set; }
    public Guid? DefaultRoleId { get; set; }
    public bool IsActive { get; set; }
    public bool AccountLockoutEnabled { get; set; }
    public int MaxFailedLoginAttempts { get; set; }
    public int LockoutDurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}


using AuthSmith.Domain.Enums;

namespace AuthSmith.Contracts.Applications;

public class CreateApplicationRequestDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SelfRegistrationMode SelfRegistrationMode { get; set; } = SelfRegistrationMode.Disabled;
    public bool AccountLockoutEnabled { get; set; } = true;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
}

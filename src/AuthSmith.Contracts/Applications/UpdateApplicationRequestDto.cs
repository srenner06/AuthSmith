using AuthSmith.Contracts.Enums;

namespace AuthSmith.Contracts.Applications;

public class UpdateApplicationRequestDto
{
    public string? Name { get; set; }
    public SelfRegistrationMode? SelfRegistrationMode { get; set; }
    public Guid? DefaultRoleId { get; set; }
    public bool? IsActive { get; set; }
    public bool? AccountLockoutEnabled { get; set; }
    public int? MaxFailedLoginAttempts { get; set; }
    public int? LockoutDurationMinutes { get; set; }
    public bool? RequireEmailVerification { get; set; }
}

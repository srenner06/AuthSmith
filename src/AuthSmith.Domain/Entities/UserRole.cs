namespace AuthSmith.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between Users and Roles.
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}


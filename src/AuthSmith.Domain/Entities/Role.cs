namespace AuthSmith.Domain.Entities;

/// <summary>
/// Represents a role defined for an application.
/// Roles are named sets of permissions.
/// </summary>
public class Role : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Admin", "Customer"
    public string? Description { get; set; }

    // Navigation properties
    public Application Application { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}


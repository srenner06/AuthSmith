using System.Diagnostics.CodeAnalysis;

namespace AuthSmith.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between Roles and Permissions.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "RolePermission is a valid domain entity name")]
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}


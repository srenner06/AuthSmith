using System.Diagnostics.CodeAnalysis;

namespace AuthSmith.Domain.Entities;

/// <summary>
/// Represents a permission defined for an application.
/// Permissions are defined per application and (module, action) pair.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Permission is a valid domain entity name")]
public class Permission : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public string Module { get; set; } = string.Empty; // e.g., "Catalog"
    public string Action { get; set; } = string.Empty; // e.g., "Read"
    public string Code { get; set; } = string.Empty; // e.g., "shopflow.catalog.read" (unique)
    public string? Description { get; set; }

    // Navigation properties
    public Application Application { get; set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}


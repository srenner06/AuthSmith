using System.Diagnostics.CodeAnalysis;

namespace AuthSmith.Domain.Entities;

/// <summary>
/// Represents a direct permission grant to a user (bypassing roles).
/// Used rarely for special cases.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "UserPermission is a valid domain entity name")]
public class UserPermission
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}


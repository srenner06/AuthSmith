using AuthSmith.Domain.Enums;

namespace AuthSmith.Domain.Entities;

/// <summary>
/// Audit log entry for tracking security and user events.
/// </summary>
public class AuditLog : BaseEntity
{
    public AuditEventType EventType { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string? UserName { get; set; }
    public string? ApplicationKey { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; } // JSON serialized additional data
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Application? Application { get; set; }
}

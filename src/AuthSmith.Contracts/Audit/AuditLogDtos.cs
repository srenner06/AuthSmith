namespace AuthSmith.Contracts.Audit;

/// <summary>
/// Audit log query request.
/// </summary>
public class AuditLogQueryDto
{
    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Filter by application ID.
    /// </summary>
    public Guid? ApplicationId { get; init; }

    /// <summary>
    /// Filter by event type.
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>
    /// Start date for filtering.
    /// </summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// End date for filtering.
    /// </summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// Audit log entry DTO.
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; init; }
    public required string EventType { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public Guid? ApplicationId { get; init; }
    public string? ApplicationKey { get; init; }
    public string? IpAddress { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Paginated audit log response.
/// </summary>
public class AuditLogPageDto
{
    public required List<AuditLogDto> Logs { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

using AuthSmith.Contracts.Audit;
using Refit;

namespace AuthSmith.Sdk.Audit;

/// <summary>
/// Client for audit log endpoints.
/// </summary>
public interface IAuditClient
{
    /// <summary>
    /// Retrieve paginated audit logs (admin only).
    /// </summary>
    [Get("/api/v1/audit/logs")]
    Task<AuditLogPageDto> GetAuditLogsAsync(
        [Query] int page = 1,
        [Query] int pageSize = 50,
        [Query] int? eventType = null,
        [Query] Guid? userId = null,
        [Query] Guid? applicationId = null,
        [Query] DateTime? startDate = null,
        [Query] DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve audit logs for a specific user (admin only).
    /// </summary>
    [Get("/api/v1/audit/users/{userId}/logs")]
    Task<List<AuditLogDto>> GetUserAuditLogsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve audit logs for a specific application (admin only).
    /// </summary>
    [Get("/api/v1/audit/applications/{applicationId}/logs")]
    Task<List<AuditLogDto>> GetApplicationAuditLogsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}

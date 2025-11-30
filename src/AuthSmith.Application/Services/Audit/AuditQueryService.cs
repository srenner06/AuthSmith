using AuthSmith.Contracts.Audit;
using AuthSmith.Domain.Enums;
using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AuthSmith.Application.Services.Audit;

/// <summary>
/// Service for querying audit logs.
/// </summary>
public interface IAuditQueryService
{
    /// <summary>
    /// Query audit logs with filtering and pagination.
    /// </summary>
    Task<AuditLogPageDto> QueryLogsAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default);
}

public class AuditQueryService : IAuditQueryService
{
    private readonly AuthSmithDbContext _dbContext;

    public AuditQueryService(AuthSmithDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditLogPageDto> QueryLogsAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var logsQuery = _dbContext.AuditLogs.AsQueryable();

        // Apply filters
        if (query.UserId.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.UserId == query.UserId.Value);
        }

        if (query.ApplicationId.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.ApplicationId == query.ApplicationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EventType) &&
            Enum.TryParse<AuditEventType>(query.EventType, out var eventType))
        {
            logsQuery = logsQuery.Where(l => l.EventType == eventType);
        }

        if (query.StartDate.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.CreatedAt <= query.EndDate.Value);
        }

        // Get total count
        var totalCount = await logsQuery.CountAsync(cancellationToken);

        // Apply pagination
        var logs = await logsQuery
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new AuditLogDto
            {
                Id = l.Id,
                EventType = l.EventType.ToString(),
                UserId = l.UserId,
                UserName = l.UserName,
                ApplicationId = l.ApplicationId,
                ApplicationKey = l.ApplicationKey,
                IpAddress = l.IpAddress,
                Success = l.Success,
                ErrorMessage = l.ErrorMessage,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new AuditLogPageDto
        {
            Logs = logs,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = totalPages
        };
    }
}

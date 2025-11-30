using System.Text.Json;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Enums;
using AuthSmith.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AuthSmith.Application.Services.Audit;

/// <summary>
/// Service for audit logging.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log an audit event.
    /// </summary>
    Task LogAsync(
        AuditEventType eventType,
        Guid? userId,
        Guid? applicationId,
        string? ipAddress,
        string? userAgent,
        bool success,
        object? details = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        AuthSmithDbContext dbContext,
        ILogger<AuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogAsync(
        AuditEventType eventType,
        Guid? userId,
        Guid? applicationId,
        string? ipAddress,
        string? userAgent,
        bool success,
        object? details = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user and application info for denormalization (faster queries)
            string? userName = null;
            string? applicationKey = null;

            if (userId.HasValue)
            {
                var user = await _dbContext.Users.FindAsync([userId.Value], cancellationToken);
                userName = user?.UserName;
            }

            if (applicationId.HasValue)
            {
                var application = await _dbContext.Applications.FindAsync([applicationId.Value], cancellationToken);
                applicationKey = application?.Key;
            }

            var auditLog = new AuditLog
            {
                EventType = eventType,
                UserId = userId,
                ApplicationId = applicationId,
                UserName = userName,
                ApplicationKey = applicationKey,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                ErrorMessage = errorMessage
            };

            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log created: {EventType} for user {UserId} - Success: {Success}",
                eventType,
                userId,
                success);
        }
        catch (Exception ex)
        {
            // Don't let audit logging failures break the application
            _logger.LogError(ex, "Failed to create audit log for event {EventType}", eventType);
        }
    }
}

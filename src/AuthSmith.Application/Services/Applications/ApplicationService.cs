using AuthSmith.Application.Mapping;
using AuthSmith.Application.Services.Audit;
using AuthSmith.Application.Services.Context;
using AuthSmith.Contracts.Applications;
using AuthSmith.Domain.Enums;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using AuthSmith.Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using App = AuthSmith.Domain.Entities.Application;

namespace AuthSmith.Application.Services.Applications;

/// <summary>
/// Service for managing applications in the system.
/// </summary>
public interface IApplicationService
{
    /// <summary>
    /// Creates a new application. Returns conflict error if application key already exists.
    /// </summary>
    Task<OneOf<ApplicationDto, ConflictError>> CreateAsync(CreateApplicationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all applications in the system.
    /// </summary>
    Task<List<ApplicationDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an application by its unique identifier.
    /// </summary>
    Task<OneOf<ApplicationDto, NotFoundError>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing application. Only provided fields are updated.
    /// </summary>
    Task<OneOf<ApplicationDto, NotFoundError>> UpdateAsync(Guid id, UpdateApplicationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new API key for an application. Returns the plain-text key (only shown once).
    /// </summary>
    Task<OneOf<string, NotFoundError>> GenerateApiKeyAsync(Guid id, CancellationToken cancellationToken = default);
}

public partial class ApplicationService : IApplicationService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IApiKeyHasher _apiKeyHasher;
    private readonly IPermissionCache _permissionCache;
    private readonly IAuditService _auditService;
    private readonly IRequestContextService _requestContext;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(
        AuthSmithDbContext dbContext,
        IApiKeyHasher apiKeyHasher,
        IPermissionCache permissionCache,
        IAuditService auditService,
        IRequestContextService requestContext,
        ILogger<ApplicationService> logger)
    {
        _dbContext = dbContext;
        _apiKeyHasher = apiKeyHasher;
        _permissionCache = permissionCache;
        _auditService = auditService;
        _requestContext = requestContext;
        _logger = logger;
    }

    public async Task<OneOf<ApplicationDto, ConflictError>> CreateAsync(CreateApplicationRequestDto request, CancellationToken cancellationToken = default)
    {
        // Convert to lowercase for case-insensitive comparison
        // ToLower() translates to SQL LOWER() function in both PostgreSQL and InMemory
        // We cannot use ToLowerInvariant() or ToLower(CultureInfo.InvariantCulture) as they don't translate to SQL
#pragma warning disable CA1304, CA1311 // Specify CultureInfo / Specify a culture
        var lowerKey = request.Key.ToLower();
        var existing = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Key.ToLower() == lowerKey, cancellationToken);
#pragma warning restore CA1304, CA1311

        if (existing != null)
            return new ConflictError($"Application with key '{request.Key}' already exists.");

        var application = new App
        {
            Key = request.Key.ToLowerInvariant(), // ToLowerInvariant is fine here - not in query
            Name = request.Name,
            SelfRegistrationMode = request.SelfRegistrationMode.ToDomain(),
            AccountLockoutEnabled = request.AccountLockoutEnabled,
            MaxFailedLoginAttempts = request.MaxFailedLoginAttempts,
            LockoutDurationMinutes = request.LockoutDurationMinutes,
            RequireEmailVerification = request.RequireEmailVerification,
            IsActive = true
        };

        _dbContext.Applications.Add(application);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogCreatedApplication(_logger, application.Id, application.Key);

        // Audit log application created
        await _auditService.LogAsync(
            AuditEventType.ApplicationCreated,
            userId: _requestContext.GetCurrentUserId(),
            application.Id,
            ipAddress: _requestContext.GetClientIpAddress(),
            userAgent: _requestContext.GetUserAgent(),
            success: true,
            details: new { key = application.Key, name = application.Name },
            cancellationToken: cancellationToken);

        return MapToDto(application);
    }

    public async Task<List<ApplicationDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _dbContext.Applications
            .OrderBy(a => a.Key)
            .ToListAsync(cancellationToken);

        return [.. applications.Select(MapToDto)];
    }

    public async Task<OneOf<ApplicationDto, NotFoundError>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (application == null)
            return new NotFoundError { Message = "Application not found." };

        return MapToDto(application);
    }

    public async Task<OneOf<ApplicationDto, NotFoundError>> UpdateAsync(Guid id, UpdateApplicationRequestDto request, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (application == null)
            return new NotFoundError { Message = "Application not found." };

        if (request.Name != null)
            application.Name = request.Name;

        if (request.SelfRegistrationMode.HasValue)
            application.SelfRegistrationMode = request.SelfRegistrationMode.Value.ToDomain();

        if (request.DefaultRoleId.HasValue)
        {
            var role = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.Id == request.DefaultRoleId.Value && r.ApplicationId == id, cancellationToken);
            if (role == null)
                return new NotFoundError { Message = "Role not found or does not belong to this application." };
            application.DefaultRoleId = request.DefaultRoleId.Value;
        }

        if (request.IsActive.HasValue)
            application.IsActive = request.IsActive.Value;

        if (request.AccountLockoutEnabled.HasValue)
            application.AccountLockoutEnabled = request.AccountLockoutEnabled.Value;

        if (request.MaxFailedLoginAttempts.HasValue)
            application.MaxFailedLoginAttempts = request.MaxFailedLoginAttempts.Value;

        if (request.LockoutDurationMinutes.HasValue)
            application.LockoutDurationMinutes = request.LockoutDurationMinutes.Value;

        if (request.RequireEmailVerification.HasValue)
            application.RequireEmailVerification = request.RequireEmailVerification.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _permissionCache.InvalidateApplicationPermissionsAsync(application.Id, cancellationToken);

        // Audit log application updated
        await _auditService.LogAsync(
            AuditEventType.ApplicationUpdated,
            userId: _requestContext.GetCurrentUserId(),
            application.Id,
            ipAddress: _requestContext.GetClientIpAddress(),
            userAgent: _requestContext.GetUserAgent(),
            success: true,
            cancellationToken: cancellationToken);

        return MapToDto(application);
    }

    public async Task<OneOf<string, NotFoundError>> GenerateApiKeyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (application == null)
            return new NotFoundError { Message = "Application not found." };

        var apiKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        application.ApiKeyHash = _apiKeyHasher.HashApiKey(apiKey);

        await _dbContext.SaveChangesAsync(cancellationToken);

        LogGeneratedApiKey(_logger, id);

        // Audit log API key generated
        await _auditService.LogAsync(
            AuditEventType.ApiKeyGenerated,
            userId: _requestContext.GetCurrentUserId(),
            application.Id,
            ipAddress: _requestContext.GetClientIpAddress(),
            userAgent: _requestContext.GetUserAgent(),
            success: true,
            cancellationToken: cancellationToken);

        return apiKey;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Created application {ApplicationId} with key {AppKey}")]
    private static partial void LogCreatedApplication(ILogger logger, Guid applicationId, string appKey);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Generated new API key for application {ApplicationId}")]
    private static partial void LogGeneratedApiKey(ILogger logger, Guid applicationId);

    private static ApplicationDto MapToDto(App application)
    {
        return new ApplicationDto
        {
            Id = application.Id,
            Key = application.Key,
            Name = application.Name,
            SelfRegistrationMode = application.SelfRegistrationMode.ToContract(),
            DefaultRoleId = application.DefaultRoleId,
            IsActive = application.IsActive,
            AccountLockoutEnabled = application.AccountLockoutEnabled,
            MaxFailedLoginAttempts = application.MaxFailedLoginAttempts,
            LockoutDurationMinutes = application.LockoutDurationMinutes,
            RequireEmailVerification = application.RequireEmailVerification,
            CreatedAt = application.CreatedAt
        };
    }
}


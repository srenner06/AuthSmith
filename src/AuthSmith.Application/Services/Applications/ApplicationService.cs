using AuthSmith.Contracts.Applications;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using AuthSmith.Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using App = AuthSmith.Domain.Entities.Application;

namespace AuthSmith.Application.Services.Applications;

public interface IApplicationService
{
    Task<OneOf<ApplicationDto, ConflictError>> CreateAsync(CreateApplicationRequestDto request, CancellationToken cancellationToken = default);
    Task<List<ApplicationDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<OneOf<ApplicationDto, NotFoundError>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OneOf<ApplicationDto, NotFoundError>> UpdateAsync(Guid id, UpdateApplicationRequestDto request, CancellationToken cancellationToken = default);
    Task<OneOf<string, NotFoundError>> GenerateApiKeyAsync(Guid id, CancellationToken cancellationToken = default);
}

public partial class ApplicationService : IApplicationService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IApiKeyHasher _apiKeyHasher;
    private readonly IPermissionCache _permissionCache;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(
        AuthSmithDbContext dbContext,
        IApiKeyHasher apiKeyHasher,
        IPermissionCache permissionCache,
        ILogger<ApplicationService> logger)
    {
        _dbContext = dbContext;
        _apiKeyHasher = apiKeyHasher;
        _permissionCache = permissionCache;
        _logger = logger;
    }

    public async Task<OneOf<ApplicationDto, ConflictError>> CreateAsync(CreateApplicationRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Applications
            .FirstOrDefaultAsync(a => string.Equals(a.Key, request.Key, StringComparison.OrdinalIgnoreCase), cancellationToken);

        if (existing != null)
            return new ConflictError($"Application with key '{request.Key}' already exists.");

        var application = new App
        {
            Key = request.Key.ToLowerInvariant(),
            Name = request.Name,
            SelfRegistrationMode = request.SelfRegistrationMode,
            AccountLockoutEnabled = request.AccountLockoutEnabled,
            MaxFailedLoginAttempts = request.MaxFailedLoginAttempts,
            LockoutDurationMinutes = request.LockoutDurationMinutes,
            IsActive = true
        };

        _dbContext.Applications.Add(application);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogCreatedApplication(_logger, application.Id, application.Key);

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
            application.SelfRegistrationMode = request.SelfRegistrationMode.Value;

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

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _permissionCache.InvalidateApplicationPermissionsAsync(application.Id, cancellationToken);

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
            SelfRegistrationMode = application.SelfRegistrationMode,
            DefaultRoleId = application.DefaultRoleId,
            IsActive = application.IsActive,
            AccountLockoutEnabled = application.AccountLockoutEnabled,
            MaxFailedLoginAttempts = application.MaxFailedLoginAttempts,
            LockoutDurationMinutes = application.LockoutDurationMinutes,
            CreatedAt = application.CreatedAt
        };
    }
}


using AuthSmith.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthSmith.Infrastructure.Services.Authentication;

public enum ApiKeyAccessLevel
{
    None,
    App,
    Admin
}

public record ApiKeyValidationResult(bool IsValid, ApiKeyAccessLevel AccessLevel, Guid? ApplicationId = null)
{
    public static ApiKeyValidationResult Valid(ApiKeyAccessLevel accessLevel, Guid? applicationId = null)
        => new(true, accessLevel, applicationId);

    public static ApiKeyValidationResult Invalid()
        => new(false, ApiKeyAccessLevel.None);
}

/// <summary>
/// Service for validating API keys.
/// </summary>
public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validates API keys against configuration (admin/bootstrap keys) and database (application keys).
/// </summary>
public partial class ApiKeyValidator : IApiKeyValidator
{
    private readonly IOptions<ApiKeysConfiguration> _apiKeysConfig;
    private readonly AuthSmithDbContext _dbContext;
    private readonly IApiKeyHasher _apiKeyHasher;
    private readonly ILogger<ApiKeyValidator> _logger;

    public ApiKeyValidator(
        IOptions<ApiKeysConfiguration> apiKeysConfig,
        AuthSmithDbContext dbContext,
        IApiKeyHasher apiKeyHasher,
        ILogger<ApiKeyValidator> logger)
    {
        _apiKeysConfig = apiKeysConfig;
        _dbContext = dbContext;
        _apiKeyHasher = apiKeyHasher;
        _logger = logger;
    }

    public async Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return ApiKeyValidationResult.Invalid();

        // Check configuration for admin keys
        var config = _apiKeysConfig.Value;
        if (config.Admin.Contains(apiKey))
        {
            LogApiKeyValidatedAsAdmin(_logger);
            return ApiKeyValidationResult.Valid(ApiKeyAccessLevel.Admin);
        }

        // Check database for application keys
        var applications = await _dbContext.Applications
            .Where(a => a.ApiKeyHash != null && a.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var app in applications)
        {
            if (app.ApiKeyHash != null && _apiKeyHasher.VerifyApiKey(apiKey, app.ApiKeyHash))
            {
                LogApiKeyValidatedForApplication(_logger, app.Id, app.Key);
                return ApiKeyValidationResult.Valid(ApiKeyAccessLevel.App, app.Id);
            }
        }

        LogInvalidApiKeyAttempted(_logger);
        return ApiKeyValidationResult.Invalid();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "API key validated as admin key")]
    private static partial void LogApiKeyValidatedAsAdmin(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "API key validated for application {ApplicationId} ({ApplicationKey})")]
    private static partial void LogApiKeyValidatedForApplication(ILogger logger, Guid applicationId, string applicationKey);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Invalid API key attempted")]
    private static partial void LogInvalidApiKeyAttempted(ILogger logger);
}


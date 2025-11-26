using System.Security.Cryptography;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;

namespace AuthSmith.Infrastructure.Services.Tokens;

/// <summary>
/// Service for managing refresh tokens.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token for a user and application.
    /// </summary>
    Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token.
    /// </summary>
    Task<OneOf<RefreshToken, UnauthorizedError>> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    Task RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user and application.
    /// </summary>
    Task RevokeAllRefreshTokensAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Refresh token service with revocation support.
/// </summary>
public partial class RefreshTokenService : IRefreshTokenService
{
    private readonly AuthSmith.Infrastructure.AuthSmithDbContext _dbContext;
    private readonly IOptions<JwtConfiguration> _jwtConfig;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        AuthSmith.Infrastructure.AuthSmithDbContext dbContext,
        IOptions<JwtConfiguration> jwtConfig,
        ILogger<RefreshTokenService> logger)
    {
        _dbContext = dbContext;
        _jwtConfig = jwtConfig;
        _logger = logger;
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddDays(_jwtConfig.Value.RefreshTokenExpirationDays);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            ApplicationId = applicationId,
            Token = token,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogGeneratedRefreshToken(_logger, userId, applicationId);

        return refreshToken;
    }

    public async Task<OneOf<RefreshToken, UnauthorizedError>> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .Include(rt => rt.Application)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (refreshToken == null)
        {
            LogRefreshTokenNotFound(_logger, token);
            return new UnauthorizedError("Invalid or expired refresh token.");
        }

        if (refreshToken.IsRevoked)
        {
            LogRefreshTokenRevoked(_logger, token);
            return new UnauthorizedError("Refresh token has been revoked.");
        }

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            LogRefreshTokenExpired(_logger, token);
            return new UnauthorizedError("Refresh token has expired.");
        }

        if (!refreshToken.User.IsActive)
        {
            LogUserNotActive(_logger, token);
            return new UnauthorizedError("User account is not active.");
        }

        if (!refreshToken.Application.IsActive)
        {
            LogApplicationNotActive(_logger, token);
            return new UnauthorizedError("Application is not active.");
        }

        // Update last used timestamp
        refreshToken.LastUsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    public async Task RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            LogRevokedRefreshToken(_logger, token);
        }
    }

    public async Task RevokeAllRefreshTokensAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var refreshTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.ApplicationId == applicationId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        LogRevokedAllRefreshTokens(_logger, userId, applicationId);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Generated refresh token for user {UserId} and application {ApplicationId}")]
    private static partial void LogGeneratedRefreshToken(ILogger logger, Guid userId, Guid applicationId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Refresh token not found: {Token}")]
    private static partial void LogRefreshTokenNotFound(ILogger logger, string token);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Refresh token is revoked: {Token}")]
    private static partial void LogRefreshTokenRevoked(ILogger logger, string token);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Refresh token has expired: {Token}")]
    private static partial void LogRefreshTokenExpired(ILogger logger, string token);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "User is not active for refresh token: {Token}")]
    private static partial void LogUserNotActive(ILogger logger, string token);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Application is not active for refresh token: {Token}")]
    private static partial void LogApplicationNotActive(ILogger logger, string token);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Revoked refresh token: {Token}")]
    private static partial void LogRevokedRefreshToken(ILogger logger, string token);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Revoked all refresh tokens for user {UserId} and application {ApplicationId}")]
    private static partial void LogRevokedAllRefreshTokens(ILogger logger, Guid userId, Guid applicationId);

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}


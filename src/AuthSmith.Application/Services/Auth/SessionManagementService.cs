using AuthSmith.Contracts.Auth;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;

namespace AuthSmith.Application.Services.Auth;

/// <summary>
/// Service for managing user sessions (refresh tokens).
/// </summary>
public interface ISessionManagementService
{
    /// <summary>
    /// Get all active sessions for a user.
    /// </summary>
    Task<OneOf<UserSessionsDto, NotFoundError>> GetUserSessionsAsync(
        Guid userId,
        Guid? currentTokenId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a specific session.
    /// </summary>
    Task<OneOf<Success, NotFoundError, UnauthorizedError>> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all sessions except the current one.
    /// </summary>
    Task<OneOf<Success, NotFoundError, UnauthorizedError>> RevokeAllSessionsAsync(
        Guid userId,
        Guid? currentTokenId,
        string password,
        CancellationToken cancellationToken = default);
}

public class SessionManagementService : ISessionManagementService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SessionManagementService> _logger;

    public SessionManagementService(
        AuthSmithDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<SessionManagementService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<OneOf<UserSessionsDto, NotFoundError>> GetUserSessionsAsync(
        Guid userId,
        Guid? currentTokenId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return new NotFoundError("User not found");
        }

        var now = DateTimeOffset.UtcNow;
        var sessions = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > now)
            .OrderByDescending(t => t.LastUsedAt ?? t.CreatedAt)
            .Select(t => new UserSessionDto
            {
                Id = t.Id,
                DeviceInfo = t.DeviceInfo ?? "Unknown Device",
                IpAddress = t.IpAddress,
                CreatedAt = t.CreatedAt,
                LastUsedAt = t.LastUsedAt,
                ExpiresAt = t.ExpiresAt,
                IsCurrentSession = currentTokenId.HasValue && t.Id == currentTokenId.Value
            })
            .ToListAsync(cancellationToken);

        return new UserSessionsDto
        {
            Sessions = sessions,
            TotalCount = sessions.Count
        };
    }

    public async Task<OneOf<Success, NotFoundError, UnauthorizedError>> RevokeSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId, cancellationToken);

        if (refreshToken == null)
        {
            return new NotFoundError("Session not found");
        }

        if (refreshToken.IsRevoked)
        {
            return new UnauthorizedError("Session already revoked");
        }

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Session {SessionId} revoked for user {UserId}", sessionId, userId);

        return Success.Instance;
    }

    public async Task<OneOf<Success, NotFoundError, UnauthorizedError>> RevokeAllSessionsAsync(
        Guid userId,
        Guid? currentTokenId,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return new NotFoundError("User not found");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return new UnauthorizedError("Password is incorrect");
        }

        // Get all active refresh tokens except current
        var tokensToRevoke = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .Where(t => !currentTokenId.HasValue || t.Id != currentTokenId.Value)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var token in tokensToRevoke)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("All sessions revoked for user {UserId} except current", userId);

        return Success.Instance;
    }
}

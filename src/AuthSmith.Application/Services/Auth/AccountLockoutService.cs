using AuthSmith.Domain.Entities;
using AuthSmith.Infrastructure;
using Microsoft.Extensions.Logging;
using App = AuthSmith.Domain.Entities.Application;

namespace AuthSmith.Application.Services.Auth;

/// <summary>
/// Service for handling account lockout logic (configurable per application).
/// </summary>
public interface IAccountLockoutService
{
    /// <summary>
    /// Checks if an account is currently locked.
    /// </summary>
    Task<bool> IsAccountLockedAsync(User user, App application, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed login attempt and locks the account if threshold is reached.
    /// </summary>
    Task RecordFailedLoginAttemptAsync(User user, App application, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets failed login attempts on successful login.
    /// </summary>
    Task ResetFailedLoginAttemptsAsync(User user, CancellationToken cancellationToken = default);
}

/// <summary>
/// Account lockout service implementation with per-application configuration.
/// </summary>
public partial class AccountLockoutService : IAccountLockoutService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly ILogger<AccountLockoutService> _logger;

    public AccountLockoutService(
        AuthSmithDbContext dbContext,
        ILogger<AccountLockoutService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<bool> IsAccountLockedAsync(User user, App application, CancellationToken cancellationToken = default)
    {
        if (!application.AccountLockoutEnabled)
            return Task.FromResult(false);

        if (user.LockedUntil == null)
            return Task.FromResult(false);

        var isLocked = user.LockedUntil > DateTimeOffset.UtcNow;
        return Task.FromResult(isLocked);
    }

    public async Task RecordFailedLoginAttemptAsync(User user, App application, CancellationToken cancellationToken = default)
    {
        if (!application.AccountLockoutEnabled)
            return;

        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= application.MaxFailedLoginAttempts)
        {
            user.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(application.LockoutDurationMinutes);
            LogAccountLocked(_logger, user.Id, user.FailedLoginAttempts);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetFailedLoginAttemptsAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user.FailedLoginAttempts > 0 || user.LockedUntil != null)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Account locked for user {UserId} due to {Attempts} failed login attempts")]
    private static partial void LogAccountLocked(ILogger logger, Guid userId, int attempts);
}


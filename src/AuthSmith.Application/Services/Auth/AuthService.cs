using AuthSmith.Contracts.Auth;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using AuthSmith.Infrastructure.Services.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using App = AuthSmith.Domain.Entities.Application;

namespace AuthSmith.Application.Services.Auth;

/// <summary>
/// Service for user authentication operations including registration, login, and token management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user for the specified application. Returns authentication tokens on success.
    /// </summary>
    /// <param name="appKey">Application key</param>
    /// <param name="request">Registration request data</param>
    /// <param name="requiresSelfRegistrationEnabled">If true, validates that self-registration is enabled. Set to false for programmatic registration.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<OneOf<AuthResultDto, NotFoundError, InvalidOperationError>> RegisterAsync(string appKey, RegisterRequestDto request, bool requiresSelfRegistrationEnabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    Task<OneOf<AuthResultDto, NotFoundError, UnauthorizedError>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    Task<OneOf<AuthResultDto, UnauthorizedError, NotFoundError>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token, preventing its future use.
    /// </summary>
    Task<OneOf<Success, NotFoundError>> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public partial class AuthService : IAuthService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccountLockoutService _accountLockoutService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthSmithDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IAccountLockoutService accountLockoutService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _accountLockoutService = accountLockoutService;
        _logger = logger;
    }

    public async Task<OneOf<AuthResultDto, NotFoundError, InvalidOperationError>> RegisterAsync(string appKey, RegisterRequestDto request, bool requiresSelfRegistrationEnabled, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Key == appKey && a.IsActive, cancellationToken);

        if (application == null)
            return new NotFoundError($"Application '{appKey}' not found or inactive.");

        // Check if self-registration is required and enabled
        if (requiresSelfRegistrationEnabled && application.SelfRegistrationMode == Domain.Enums.SelfRegistrationMode.Disabled)
            return new InvalidOperationError($"Self-registration is not enabled for application '{appKey}'.");

        var normalizedUserName = request.Username.ToUpperInvariant();
        var normalizedEmail = request.Email.ToUpperInvariant();

        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName || u.NormalizedEmail == normalizedEmail, cancellationToken);

        User user;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (existingUser != null)
            {
                user = existingUser;
                LogExistingUserRegistering(_logger, user.Id, appKey);
            }
            else
            {
                user = new User
                {
                    UserName = request.Username,
                    NormalizedUserName = normalizedUserName,
                    Email = request.Email,
                    NormalizedEmail = normalizedEmail,
                    PasswordHash = _passwordHasher.HashPassword(request.Password),
                    IsActive = true
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
                LogCreatedNewUser(_logger, user.Id, appKey);
            }

            if (application.DefaultRoleId.HasValue)
            {
                var existingRole = await _dbContext.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == application.DefaultRoleId.Value, cancellationToken);

                if (existingRole == null)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = application.DefaultRoleId.Value
                    });
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var authResult = await GenerateAuthResultAsync(user, application, cancellationToken);
        return authResult.Match<OneOf<AuthResultDto, NotFoundError, InvalidOperationError>>(
            result => result,
            notFoundError => notFoundError,
            fileNotFoundError => new NotFoundError { Message = fileNotFoundError.Message }
        );
    }

    public async Task<OneOf<AuthResultDto, NotFoundError, UnauthorizedError>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Key == request.AppKey && a.IsActive, cancellationToken);

        if (application == null)
            return new NotFoundError($"Application '{request.AppKey}' not found or inactive.");

        var normalizedInput = request.UsernameOrEmail.ToUpperInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedInput || u.NormalizedEmail == normalizedInput, cancellationToken);

        if (user == null || !user.IsActive)
        {
            LogLoginAttemptInvalidUsername(_logger, request.UsernameOrEmail);
            return new UnauthorizedError("Invalid credentials.");
        }

        if (await _accountLockoutService.IsAccountLockedAsync(user, application, cancellationToken))
        {
            LogLoginAttemptLockedAccount(_logger, user.Id);
            return new UnauthorizedError("Account is locked. Please try again later.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            await _accountLockoutService.RecordFailedLoginAttemptAsync(user, application, cancellationToken);
            LogInvalidPassword(_logger, user.Id);
            return new UnauthorizedError("Invalid credentials.");
        }

        await _accountLockoutService.ResetFailedLoginAttemptsAsync(user, cancellationToken);
        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        LogSuccessfulLogin(_logger, user.Id, request.AppKey);

        var authResult = await GenerateAuthResultAsync(user, application, cancellationToken);
        return authResult.Match<OneOf<AuthResultDto, NotFoundError, UnauthorizedError>>(
            result => result,
            notFoundError => notFoundError,
            fileNotFoundError => new NotFoundError { Message = fileNotFoundError.Message }
        );
    }

    public async Task<OneOf<AuthResultDto, UnauthorizedError, NotFoundError>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenResult = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
        return await tokenResult.Match(
            async token =>
            {
                var user = await _dbContext.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);

                if (user == null || !user.IsActive)
                {
                    UnauthorizedError error = new("User not found or inactive.");
                    return (OneOf<AuthResultDto, UnauthorizedError, NotFoundError>)error;
                }

                var application = await _dbContext.Applications
                    .FirstOrDefaultAsync(a => a.Id == token.ApplicationId, cancellationToken);

                if (application == null || !application.IsActive)
                {
                    NotFoundError error = new("Application not found or inactive.");
                    return (OneOf<AuthResultDto, UnauthorizedError, NotFoundError>)error;
                }

                LogRefreshingToken(_logger, user.Id, application.Key);

                var authResult = await GenerateAuthResultAsync(user, application, cancellationToken);
                return authResult.Match<OneOf<AuthResultDto, UnauthorizedError, NotFoundError>>(
                    result => result,
                    notFoundError => notFoundError,
                    fileNotFoundError => new NotFoundError(fileNotFoundError.Message)
                );
            },
            unauthorizedError => Task.FromResult<OneOf<AuthResultDto, UnauthorizedError, NotFoundError>>((OneOf<AuthResultDto, UnauthorizedError, NotFoundError>)unauthorizedError)
        );
    }

    public async Task<OneOf<Success, NotFoundError>> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
        LogRefreshTokenRevoked(_logger);
        return Success.Instance;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Existing user {UserId} registering for application {AppKey}")]
    private static partial void LogExistingUserRegistering(ILogger logger, Guid userId, string appKey);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Created new user {UserId} for application {AppKey}")]
    private static partial void LogCreatedNewUser(ILogger logger, Guid userId, string appKey);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Login attempt with invalid username/email: {Input}")]
    private static partial void LogLoginAttemptInvalidUsername(ILogger logger, string input);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Login attempt for locked account: {UserId}")]
    private static partial void LogLoginAttemptLockedAccount(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Invalid password for user {UserId}")]
    private static partial void LogInvalidPassword(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Successful login for user {UserId} in application {AppKey}")]
    private static partial void LogSuccessfulLogin(ILogger logger, Guid userId, string appKey);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Refreshing token for user {UserId} in application {AppKey}")]
    private static partial void LogRefreshingToken(ILogger logger, Guid userId, string appKey);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Refresh token revoked")]
    private static partial void LogRefreshTokenRevoked(ILogger logger);

    private async Task<OneOf<AuthResultDto, NotFoundError, FileNotFoundError>> GenerateAuthResultAsync(User user, App application, CancellationToken cancellationToken)
    {
        // Single query to get roles and role IDs
        var userRoles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_dbContext.Roles.Where(r => r.ApplicationId == application.Id),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { r.Id, r.Name })
            .ToListAsync(cancellationToken);

        var roles = userRoles.Select(r => r.Name).ToList();
        var roleIds = userRoles.Select(r => r.Id).ToList();

        // Single query to get all permissions (from roles and direct) using Union
        var permissionsFromRoles = _dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_dbContext.Permissions,
                rp => rp.PermissionId,
                p => p.Id,
                (rp, p) => p.Code);

        var directPermissions = _dbContext.UserPermissions
            .Where(up => up.UserId == user.Id)
            .Join(_dbContext.Permissions.Where(p => p.ApplicationId == application.Id),
                up => up.PermissionId,
                p => p.Id,
                (up, p) => p.Code);

        var allPermissions = await permissionsFromRoles
            .Union(directPermissions)
            .Distinct()
            .ToListAsync(cancellationToken);

        var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(
            user, application, roles, allPermissions, cancellationToken);

        return await accessTokenResult.Match(
            async accessToken =>
            {
                var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id, application.Id, cancellationToken);
                var result = new AuthResultDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresIn = 900
                };
                return (OneOf<AuthResultDto, NotFoundError, FileNotFoundError>)result;
            },
            notFoundError => Task.FromResult<OneOf<AuthResultDto, NotFoundError, FileNotFoundError>>(notFoundError),
            fileNotFoundError => Task.FromResult<OneOf<AuthResultDto, NotFoundError, FileNotFoundError>>(fileNotFoundError)
        );
    }
}

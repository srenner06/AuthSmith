using AuthSmith.Contracts.Users;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using AuthSmith.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;

namespace AuthSmith.Application.Services.Users;

/// <summary>
/// Service for user profile management.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Get user profile by ID.
    /// </summary>
    Task<OneOf<UserProfileDto, NotFoundError>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user profile.
    /// </summary>
    Task<OneOf<UserProfileDto, NotFoundError, ConflictError, ValidationError>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Change user password.
    /// </summary>
    Task<OneOf<Success, NotFoundError, UnauthorizedError, ValidationError>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete user account.
    /// </summary>
    Task<OneOf<Success, NotFoundError, UnauthorizedError, ValidationError>> DeleteAccountAsync(
        Guid userId,
        DeleteAccountDto request,
        CancellationToken cancellationToken = default);
}

public class UserProfileService : IUserProfileService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService? _emailService;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        AuthSmithDbContext dbContext,
        IPasswordHasher passwordHasher,
        IEmailService? emailService,
        ILogger<UserProfileService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<OneOf<UserProfileDto, NotFoundError>> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return new NotFoundError("User not found");
        }

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<OneOf<UserProfileDto, NotFoundError, ConflictError, ValidationError>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return new NotFoundError("User not found");
        }

        var hasChanges = false;

        // Update username if provided
        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
        {
            // Check if username is taken
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == request.UserName.ToUpperInvariant(), cancellationToken);

            if (existingUser != null)
            {
                return new ConflictError("Username is already taken");
            }

            user.UserName = request.UserName;
            user.NormalizedUserName = request.UserName.ToUpperInvariant();
            hasChanges = true;
        }

        // Update email if provided
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            // Check if email is taken
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpperInvariant(), cancellationToken);

            if (existingUser != null)
            {
                return new ConflictError("Email is already taken");
            }

            user.Email = request.Email;
            user.NormalizedEmail = request.Email.ToUpperInvariant();
            user.EmailVerified = false; // Require re-verification
            user.EmailVerifiedAt = null;
            hasChanges = true;

            _logger.LogInformation("User {UserId} changed email, verification required", userId);
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<OneOf<Success, NotFoundError, UnauthorizedError, ValidationError>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate new password
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return new ValidationError([
                new ValidationErrorDetail
                {
                    PropertyName = nameof(request.NewPassword),
                    ErrorMessage = "Password must be at least 8 characters long",
                    AttemptedValue = request.NewPassword
                }
            ]);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return new NotFoundError("User not found");
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return new UnauthorizedError("Current password is incorrect");
        }

        // Update password
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send notification email (async, don't wait) - only if email service is configured
        if (_emailService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordChangedEmailAsync(user.Email, user.UserName, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password changed email to user {UserId}", userId);
                }
            }, cancellationToken);
        }

        _logger.LogInformation("Password changed for user {UserId}", userId);

        return Success.Instance;
    }

    public async Task<OneOf<Success, NotFoundError, UnauthorizedError, ValidationError>> DeleteAccountAsync(
        Guid userId,
        DeleteAccountDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate confirmation
        if (request.Confirmation != "DELETE")
        {
            return new ValidationError([
                new ValidationErrorDetail
                {
                    PropertyName = nameof(request.Confirmation),
                    ErrorMessage = "Confirmation must be 'DELETE'",
                    AttemptedValue = request.Confirmation
                }
            ]);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return new NotFoundError("User not found");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new UnauthorizedError("Password is incorrect");
        }

        // Soft delete: deactivate instead of hard delete
        user.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("User account deleted: {UserId}", userId);

        return Success.Instance;
    }
}

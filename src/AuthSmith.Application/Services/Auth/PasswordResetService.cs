using System.Security.Cryptography;
using AuthSmith.Contracts.Auth;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using AuthSmith.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;

namespace AuthSmith.Application.Services.Auth;

/// <summary>
/// Service for password reset functionality.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Request a password reset token.
    /// </summary>
    Task<OneOf<PasswordResetResponseDto, NotFoundError, ValidationError>> RequestPasswordResetAsync(
        PasswordResetRequestDto request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm password reset with token.
    /// </summary>
    Task<OneOf<PasswordResetResponseDto, NotFoundError, ValidationError>> ConfirmPasswordResetAsync(
        PasswordResetConfirmDto request,
        CancellationToken cancellationToken = default);
}

public class PasswordResetService : IPasswordResetService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService? _emailService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        AuthSmithDbContext dbContext,
        IPasswordHasher passwordHasher,
        IEmailService? emailService,
        ILogger<PasswordResetService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<OneOf<PasswordResetResponseDto, NotFoundError, ValidationError>> RequestPasswordResetAsync(
        PasswordResetRequestDto request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Always return success to prevent email enumeration attacks
        const string successMessage = "If an account with that email exists, a password reset link has been sent.";

        // Find user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpperInvariant(), cancellationToken);

        if (user == null || !user.IsActive)
        {
            _logger.LogInformation("Password reset requested for non-existent or inactive email: {Email}", request.Email);
            // Return success to prevent email enumeration
            return new PasswordResetResponseDto { Message = successMessage };
        }

        // Verify user has access to the application
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Key == request.ApplicationKey && a.IsActive, cancellationToken);

        if (application == null)
        {
            _logger.LogWarning("Password reset requested for invalid application: {AppKey}", request.ApplicationKey);
            return new PasswordResetResponseDto { Message = successMessage };
        }

        // Generate secure token
        var token = GenerateSecureToken();
        var tokenHash = HashToken(token);

        // Invalidate any existing tokens for this user
        var existingTokens = await _dbContext.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var existingToken in existingTokens)
        {
            existingToken.IsUsed = true;
            existingToken.UsedAt = DateTimeOffset.UtcNow;
        }

        // Create new reset token
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), // 1 hour expiration
            IpAddress = ipAddress
        };

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send email (async, don't wait) - only if email service is configured
        if (_emailService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.UserName, token, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                }
            }, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Email service not configured - password reset email not sent for user {UserId}", user.Id);
        }

        _logger.LogInformation("Password reset token generated for user {UserId}", user.Id);

        return new PasswordResetResponseDto { Message = successMessage };
    }

    public async Task<OneOf<PasswordResetResponseDto, NotFoundError, ValidationError>> ConfirmPasswordResetAsync(
        PasswordResetConfirmDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate password
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return new ValidationError([
                new ValidationErrorDetail
                {
                    PropertyName = nameof(request.NewPassword),
                    ErrorMessage = "Password is required",
                    AttemptedValue = request.NewPassword
                }
            ]);
        }

        if (request.NewPassword.Length < 8)
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

        // Hash the token to find it
        var tokenHash = HashToken(request.Token);

        // Find the token
        var resetToken = await _dbContext.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);

        if (resetToken == null)
        {
            return new NotFoundError("Invalid or expired password reset token");
        }

        // Validate token
        if (!resetToken.IsValid())
        {
            return new NotFoundError("Password reset token has expired or already been used");
        }

        // Update user password
        resetToken.User.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        resetToken.User.FailedLoginAttempts = 0;
        resetToken.User.LockedUntil = null;

        // Mark token as used
        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send confirmation email (async, don't wait) - only if email service is configured
        if (_emailService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordChangedEmailAsync(
                        resetToken.User.Email,
                        resetToken.User.UserName,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password changed email to {Email}", resetToken.User.Email);
                }
            }, cancellationToken);
        }

        _logger.LogInformation("Password reset completed for user {UserId}", resetToken.UserId);

        return new PasswordResetResponseDto { Message = "Password has been reset successfully" };
    }

    private static string GenerateSecureToken()
    {
        // Generate 32 bytes (256 bits) of random data
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string HashToken(string token)
    {
        // Hash the token for storage (same as password hashing for consistency)
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

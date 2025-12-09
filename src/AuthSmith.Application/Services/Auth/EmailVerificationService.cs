using System.Security.Cryptography;
using AuthSmith.Application.Services.Audit;
using AuthSmith.Application.Services.Context;
using AuthSmith.Contracts.Auth;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Enums;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;

namespace AuthSmith.Application.Services.Auth;

/// <summary>
/// Service for email verification functionality.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Send email verification token to user.
    /// </summary>
    Task<OneOf<EmailVerificationResponseDto, NotFoundError>> SendVerificationEmailAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify email with token.
    /// </summary>
    Task<OneOf<EmailVerificationResponseDto, NotFoundError>> VerifyEmailAsync(
        VerifyEmailDto request,
        CancellationToken cancellationToken = default);
}

public class EmailVerificationService : IEmailVerificationService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IEmailService? _emailService;
    private readonly IAuditService _auditService;
    private readonly IRequestContextService _requestContext;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        AuthSmithDbContext dbContext,
        IEmailService? emailService,
        IAuditService auditService,
        IRequestContextService requestContext,
        ILogger<EmailVerificationService> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _auditService = auditService;
        _requestContext = requestContext;
        _logger = logger;
    }

    public async Task<OneOf<EmailVerificationResponseDto, NotFoundError>> SendVerificationEmailAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Always return success to prevent email enumeration
        const string successMessage = "If an account with that email exists, a verification link has been sent.";

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);

        if (user == null || !user.IsActive)
        {
            _logger.LogInformation("Email verification requested for non-existent or inactive email: {Email}", email);
            return new EmailVerificationResponseDto
            {
                Message = successMessage,
                IsVerified = false
            };
        }

        // Check if already verified
        if (user.EmailVerified)
        {
            return new EmailVerificationResponseDto
            {
                Message = "Email is already verified.",
                IsVerified = true
            };
        }

        // Generate secure token
        var token = GenerateSecureToken();
        var tokenHash = HashToken(token);

        // Invalidate any existing tokens
        var existingTokens = await _dbContext.EmailVerificationTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var existingToken in existingTokens)
        {
            existingToken.IsUsed = true;
            existingToken.UsedAt = DateTimeOffset.UtcNow;
        }

        // Create new verification token
        var verificationToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24), // 24 hour expiration
            IpAddress = ipAddress
        };

        _dbContext.EmailVerificationTokens.Add(verificationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Audit log email verification sent
        await _auditService.LogAsync(
            AuditEventType.EmailVerificationSent,
            user.Id,
            applicationId: null,
            ipAddress: _requestContext.GetClientIpAddress(),
            userAgent: _requestContext.GetUserAgent(),
            success: true,
            details: new { email = user.Email },
            cancellationToken: cancellationToken);

        // Send email (async, don't wait) - only if email service is configured
        if (_emailService != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailVerificationAsync(user.Email, user.UserName, token, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email verification to {Email}", user.Email);
                }
            }, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Email service not configured - verification email not sent for user {UserId}", user.Id);
        }

        _logger.LogInformation("Email verification token sent to user {UserId}", user.Id);

        return new EmailVerificationResponseDto
        {
            Message = successMessage,
            IsVerified = false
        };
    }

    public async Task<OneOf<EmailVerificationResponseDto, NotFoundError>> VerifyEmailAsync(
        VerifyEmailDto request,
        CancellationToken cancellationToken = default)
    {
        // Hash the token to find it
        var tokenHash = HashToken(request.Token);

        // Find the token with tracking
        var verificationToken = await _dbContext.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);

        if (verificationToken == null)
        {
            return new NotFoundError("Invalid or expired verification token");
        }

        // Validate token
        if (!verificationToken.IsValid())
        {
            return new NotFoundError("Verification token has expired or already been used");
        }

        // Get the user entity directly from context to ensure it's tracked
        var user = verificationToken.User;

        // Mark user as verified
        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTimeOffset.UtcNow;

        // Mark token as used
        verificationToken.IsUsed = true;
        verificationToken.UsedAt = DateTimeOffset.UtcNow;

        // Explicitly mark entities as modified
        _dbContext.Entry(user).State = EntityState.Modified;
        _dbContext.Entry(verificationToken).State = EntityState.Modified;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Audit log email verified
        await _auditService.LogAsync(
            AuditEventType.EmailVerified,
            verificationToken.UserId,
            applicationId: null,
            ipAddress: _requestContext.GetClientIpAddress(),
            userAgent: _requestContext.GetUserAgent(),
            success: true,
            details: new { email = user.Email },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Email verified for user {UserId}", verificationToken.UserId);

        return new EmailVerificationResponseDto
        {
            Message = "Email verified successfully",
            IsVerified = true
        };
    }

    private static string GenerateSecureToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

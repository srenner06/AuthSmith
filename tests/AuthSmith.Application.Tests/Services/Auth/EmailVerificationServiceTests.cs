using AuthSmith.Application.Services.Auth;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Auth;
using AuthSmith.Domain.Entities;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthSmith.Application.Tests.Services.Auth;

public class EmailVerificationServiceTests : TestBase
{
    private static AuthSmithDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthSmithDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AuthSmithDbContext(options);
    }

    private EmailVerificationService CreateService(
        AuthSmithDbContext? dbContext = null,
        Mock<IEmailService>? emailService = null,
        Mock<ILogger<EmailVerificationService>>? logger = null)
    {
        dbContext ??= CreateDbContext();
        logger ??= CreateLoggerMock<EmailVerificationService>();

        return new EmailVerificationService(
            dbContext,
            emailService?.Object,
            logger.Object);
    }

    #region SendVerificationEmailAsync Tests

    [Test]
    public async Task SendVerificationEmailAsync_ShouldReturnSuccess_WhenUserExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            email: "test@example.com",
            emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var emailService = new Mock<IEmailService>();
        emailService.Setup(x => x.SendEmailVerificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(dbContext, emailService);

        // Act
        var result = await service.SendVerificationEmailAsync("test@example.com", "192.168.1.1");

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var response = result.AsT0;
        await Assert.That(response.Message).Contains("verification link");
        await Assert.That(response.IsVerified).IsFalse();

        // Verify token was created
        var token = await dbContext.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id);
        await Assert.That(token).IsNotNull();
        await Assert.That(token!.IpAddress).IsEqualTo("192.168.1.1");
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        // Act
        var result = await service.SendVerificationEmailAsync("nonexistent@example.com", "192.168.1.1");

        // Assert - Should still return success to prevent email enumeration
        await Assert.That(result.IsT0).IsTrue();
        var response = result.AsT0;
        await Assert.That(response.Message).Contains("verification link");
        await Assert.That(response.IsVerified).IsFalse();
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldReturnAlreadyVerified_WhenEmailIsVerified()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            email: "test@example.com",
            emailVerified: true);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.SendVerificationEmailAsync("test@example.com", "192.168.1.1");

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var response = result.AsT0;
        await Assert.That(response.Message).Contains("already verified");
        await Assert.That(response.IsVerified).IsTrue();
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldInvalidateExistingTokens_WhenSendingNewToken()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            email: "test@example.com",
            emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create an existing token
        var existingToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = "old_token_hash",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            IpAddress = "192.168.1.1"
        };
        dbContext.EmailVerificationTokens.Add(existingToken);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.SendVerificationEmailAsync("test@example.com", "192.168.1.2");

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        // Verify old token was invalidated
        var oldToken = await dbContext.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Id == existingToken.Id);
        await Assert.That(oldToken!.IsUsed).IsTrue();
        await Assert.That(oldToken.UsedAt).IsNotNull();

        // Verify new token was created
        var newTokens = await dbContext.EmailVerificationTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync();
        await Assert.That(newTokens.Count).IsEqualTo(1);
        await Assert.That(newTokens[0].IpAddress).IsEqualTo("192.168.1.2");
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldWorkWithoutEmailService()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            email: "test@example.com",
            emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, emailService: null);

        // Act
        var result = await service.SendVerificationEmailAsync("test@example.com", "192.168.1.1");

        // Assert - Should still succeed even without email service
        await Assert.That(result.IsT0).IsTrue();
        var response = result.AsT0;
        await Assert.That(response.IsVerified).IsFalse();

        // Verify token was still created
        var token = await dbContext.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id);
        await Assert.That(token).IsNotNull();
    }

    #endregion

    #region VerifyEmailAsync Tests

    [Test]
    public async Task VerifyEmailAsync_ShouldSucceed_WhenTokenIsValid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            email: "test@example.com",
            emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create a valid token (we'll use a known hash for testing)
        var plainToken = "test_verification_token_123";
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(plainToken)));

        var verificationToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            IpAddress = "192.168.1.1"
        };
        dbContext.EmailVerificationTokens.Add(verificationToken);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var request = new VerifyEmailDto
        {
            Token = plainToken
        };

        // Act
        var result = await service.VerifyEmailAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var response = result.AsT0;
        await Assert.That(response.Message).Contains("successfully");
        await Assert.That(response.IsVerified).IsTrue();

        // Verify user is now verified
        var verifiedUser = await dbContext.Users.FindAsync(user.Id);
        await Assert.That(verifiedUser!.EmailVerified).IsTrue();
        await Assert.That(verifiedUser.EmailVerifiedAt).IsNotNull();

        // Verify token is marked as used
        var usedToken = await dbContext.EmailVerificationTokens.FindAsync(verificationToken.Id);
        await Assert.That(usedToken!.IsUsed).IsTrue();
        await Assert.That(usedToken.UsedAt).IsNotNull();
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldReturnNotFound_WhenTokenDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var request = new VerifyEmailDto
        {
            Token = "nonexistent_token"
        };

        // Act
        var result = await service.VerifyEmailAsync(request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("Invalid");
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldReturnNotFound_WhenTokenIsExpired()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            email: "test@example.com",
            emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var plainToken = "expired_token";
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(plainToken)));

        var expiredToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired 1 hour ago
            IsUsed = false,
            IpAddress = "192.168.1.1"
        };
        dbContext.EmailVerificationTokens.Add(expiredToken);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var request = new VerifyEmailDto
        {
            Token = plainToken
        };

        // Act
        var result = await service.VerifyEmailAsync(request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("expired");
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldReturnNotFound_WhenTokenIsAlreadyUsed()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            email: "test@example.com",
            emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var plainToken = "used_token";
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(plainToken)));

        var usedToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = true,
            UsedAt = DateTime.UtcNow.AddHours(-1),
            IpAddress = "192.168.1.1"
        };
        dbContext.EmailVerificationTokens.Add(usedToken);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var request = new VerifyEmailDto
        {
            Token = plainToken
        };

        // Act
        var result = await service.VerifyEmailAsync(request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).Contains("already been used");
    }

    #endregion
}

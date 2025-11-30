using System.Net;
using System.Net.Http.Json;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Auth;
using AuthSmith.Domain.Entities;
using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthSmith.Api.Tests.Controllers;

public class EmailVerificationControllerTests
{
    [Test]
    public async Task SendVerificationEmailAsync_ShouldReturnSuccess_WhenEmailExists()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();

        dbContext.Database.EnsureCreated();

        // Use unique email to prevent conflicts
        var uniqueEmail = $"test-{Guid.NewGuid()}@example.com";
        var user = TestDataBuilder.CreateUser(
            userName: $"testuser-{Guid.NewGuid()}",
            email: uniqueEmail,
            emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = new ResendVerificationEmailDto
        {
            Email = uniqueEmail
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/email-verification/send", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EmailVerificationResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).Contains("verification link");
        await Assert.That(result.IsVerified).IsFalse();

        // Verify token was created in database
        var token = await dbContext.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id);
        await Assert.That(token).IsNotNull();
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldReturnSuccess_EvenWhenEmailDoesNotExist()
    {
        // Arrange - Testing email enumeration protection
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();

        dbContext.Database.EnsureCreated();

        var request = new ResendVerificationEmailDto
        {
            Email = $"nonexistent-{Guid.NewGuid()}@example.com"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/email-verification/send", request);

        // Assert - Should still return 200 to prevent email enumeration
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EmailVerificationResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).Contains("verification link");
    }

    [Test]
    public async Task SendVerificationEmailAsync_ShouldReturnAlreadyVerified_WhenEmailIsVerified()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();

        dbContext.Database.EnsureCreated();

        // Use unique email
        var uniqueEmail = $"test-{Guid.NewGuid()}@example.com";
        var user = TestDataBuilder.CreateUser(
            userName: $"testuser-{Guid.NewGuid()}",
            email: uniqueEmail,
            emailVerified: true);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = new ResendVerificationEmailDto
        {
            Email = uniqueEmail
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/email-verification/send", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EmailVerificationResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).Contains("already verified");
        await Assert.That(result.IsVerified).IsTrue();
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldReturnSuccess_WhenTokenIsValid()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        Guid userId;
        var plainToken = $"test_verification_token_{Guid.NewGuid()}";

        // Setup phase - Create user and token in a separate scope
        using (var setupScope = factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
            dbContext.Database.EnsureCreated();

            var user = TestDataBuilder.CreateUser(emailVerified: false);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            userId = user.Id;

            // Create a valid token
            var tokenHash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(plainToken)));

            var verificationToken = new EmailVerificationToken
            {
                UserId = user.Id,
                Token = tokenHash,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
                IsUsed = false,
                IpAddress = "127.0.0.1"
            };
            dbContext.EmailVerificationTokens.Add(verificationToken);
            await dbContext.SaveChangesAsync();
        } // Setup scope disposed here

        var request = new VerifyEmailDto
        {
            Token = plainToken
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/email-verification/verify", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EmailVerificationResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Message).Contains("successfully");
        await Assert.That(result.IsVerified).IsTrue();

        // Verify user is now verified in database - Use a NEW scope to see the changes
        using (var verifyScope = factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
            var verifiedUser = await dbContext.Users.FindAsync(userId);
            await Assert.That(verifiedUser).IsNotNull();
            await Assert.That(verifiedUser!.EmailVerified).IsTrue();
            await Assert.That(verifiedUser.EmailVerifiedAt).IsNotNull();
        }
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldReturnNotFound_WhenTokenIsInvalid()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();

        dbContext.Database.EnsureCreated();

        var request = new VerifyEmailDto
        {
            Token = $"invalid_token_{Guid.NewGuid()}"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/email-verification/verify", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldReturnNotFound_WhenTokenIsExpired()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();

        dbContext.Database.EnsureCreated();

        var user = TestDataBuilder.CreateUser(emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create an expired token with unique value
        var plainToken = $"expired_token_{Guid.NewGuid()}";
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(plainToken)));

        var expiredToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1), // Expired 1 hour ago
            IsUsed = false,
            IpAddress = "127.0.0.1"
        };
        dbContext.EmailVerificationTokens.Add(expiredToken);
        await dbContext.SaveChangesAsync();

        var request = new VerifyEmailDto
        {
            Token = plainToken
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/email-verification/verify", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task VerifyEmailAsync_ShouldReturnNotFound_WhenTokenIsAlreadyUsed()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();

        dbContext.Database.EnsureCreated();

        var user = TestDataBuilder.CreateUser(emailVerified: false);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create an already used token with unique value
        var plainToken = $"used_token_{Guid.NewGuid()}";
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(plainToken)));

        var usedToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            IsUsed = true,
            UsedAt = DateTimeOffset.UtcNow.AddHours(-1),
            IpAddress = "127.0.0.1"
        };
        dbContext.EmailVerificationTokens.Add(usedToken);
        await dbContext.SaveChangesAsync();

        var request = new VerifyEmailDto
        {
            Token = plainToken
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/email-verification/verify", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

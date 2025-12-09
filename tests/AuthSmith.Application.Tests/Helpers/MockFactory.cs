using AuthSmith.Application.Services.Audit;
using AuthSmith.Application.Services.Context;
using AuthSmith.Domain.Enums;
using AuthSmith.Infrastructure.Services.Authentication;
using AuthSmith.Infrastructure.Services.Caching;
using AuthSmith.Infrastructure.Services.Tokens;
using Microsoft.Extensions.Logging;
using Moq;
using OneOf;

namespace AuthSmith.Application.Tests.Helpers;

/// <summary>
/// Factory for creating mocked infrastructure services.
/// </summary>
public static class MockFactory
{
    public static Mock<IPasswordHasher> CreatePasswordHasher()
    {
        var mock = new Mock<IPasswordHasher>();
        mock.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns<string>(password => $"hashed_{password}");
        mock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((password, hash) => hash == $"hashed_{password}");
        return mock;
    }

    public static Mock<IApiKeyHasher> CreateApiKeyHasher()
    {
        var mock = new Mock<IApiKeyHasher>();
        mock.Setup(x => x.HashApiKey(It.IsAny<string>()))
            .Returns<string>(key => $"hashed_{key}");
        return mock;
    }

    public static Mock<AuthSmith.Infrastructure.Services.Authentication.IJwtTokenService> CreateJwtTokenService()
    {
        var mock = new Mock<AuthSmith.Infrastructure.Services.Authentication.IJwtTokenService>();
        mock.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<AuthSmith.Domain.Entities.User>(),
                It.IsAny<AuthSmith.Domain.Entities.Application>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<string, AuthSmith.Domain.Errors.NotFoundError, AuthSmith.Domain.Errors.FileNotFoundError>)"mock_access_token");
        return mock;
    }

    public static Mock<IRefreshTokenService> CreateRefreshTokenService()
    {
        var mock = new Mock<IRefreshTokenService>();
        mock.Setup(x => x.GenerateRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthSmith.Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = It.IsAny<Guid>(),
                ApplicationId = It.IsAny<Guid>(),
                Token = "mock_refresh_token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });
        return mock;
    }

    public static Mock<IPermissionCache> CreatePermissionCache()
    {
        var mock = new Mock<IPermissionCache>();
        mock.Setup(x => x.GetUserPermissionsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HashSet<string>?)null);
        return mock;
    }

    public static Mock<IAuditService> CreateAuditService()
    {
        var mock = new Mock<IAuditService>();
        mock.Setup(x => x.LogAsync(
                It.IsAny<AuditEventType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    public static Mock<IRequestContextService> CreateRequestContextService()
    {
        var mock = new Mock<IRequestContextService>();
        mock.Setup(x => x.GetClientIpAddress()).Returns("127.0.0.1");
        mock.Setup(x => x.GetUserAgent()).Returns("Test-Agent/1.0");
        mock.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);
        return mock;
    }

    public static Mock<ILogger<T>> CreateLogger<T>() => new();
}


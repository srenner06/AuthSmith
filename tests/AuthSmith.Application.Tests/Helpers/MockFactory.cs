using AuthSmith.Infrastructure.Services.Authentication;
using AuthSmith.Infrastructure.Services.Caching;
using AuthSmith.Infrastructure.Services.Tokens;
using Microsoft.Extensions.Logging;
using OneOf;
using Moq;

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

    public static Mock<ILogger<T>> CreateLogger<T>() => new();
}


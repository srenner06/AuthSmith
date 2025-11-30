using AuthSmith.Application.Services.Auth;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Auth;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Enums;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OneOf;
using ApplicationEntity = AuthSmith.Domain.Entities.Application;

namespace AuthSmith.Application.Tests.Services.Auth;

public class AuthServiceTests : TestBase
{
    private static AuthSmithDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthSmithDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AuthSmithDbContext(options);
    }

    private AuthService CreateService(
        AuthSmithDbContext? dbContext = null,
        Mock<Infrastructure.Services.Authentication.IPasswordHasher>? passwordHasher = null,
        Mock<Infrastructure.Services.Authentication.IJwtTokenService>? jwtTokenService = null,
        Mock<Infrastructure.Services.Tokens.IRefreshTokenService>? refreshTokenService = null,
        Mock<IAccountLockoutService>? accountLockoutService = null,
        Mock<IEmailVerificationService>? emailVerificationService = null,
        Mock<ILogger<AuthService>>? logger = null)
    {
        dbContext ??= CreateDbContext();
        passwordHasher ??= Helpers.MockFactory.CreatePasswordHasher();
        jwtTokenService ??= Helpers.MockFactory.CreateJwtTokenService();
        refreshTokenService ??= Helpers.MockFactory.CreateRefreshTokenService();

        // Only create and set up default mock if one wasn't provided
        // This allows tests to provide their own mock with custom setups
        var wasMockProvided = accountLockoutService != null;
        accountLockoutService ??= new Mock<IAccountLockoutService>();
        emailVerificationService ??= new Mock<IEmailVerificationService>();
        logger ??= CreateLoggerMock<AuthService>();

        // Only set up default behavior if we created a new mock (not provided by test)
        if (!wasMockProvided)
        {
            accountLockoutService.Setup(x => x.IsAccountLockedAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }
        // These setups are safe to apply even if mock was provided, as they're utility methods
        accountLockoutService.Setup(x => x.RecordFailedLoginAttemptAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        accountLockoutService.Setup(x => x.ResetFailedLoginAttemptsAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new AuthService(
            dbContext,
            passwordHasher.Object,
            jwtTokenService.Object,
            refreshTokenService.Object,
            accountLockoutService.Object,
            emailVerificationService.Object,
            logger.Object);
    }

    [Test]
    public async Task RegisterAsync_ShouldCreateNewUser_WhenSelfRegistrationIsOpen()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(selfRegistrationMode: SelfRegistrationMode.Open);
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var passwordHasher = Helpers.MockFactory.CreatePasswordHasher();
        var jwtTokenService = Helpers.MockFactory.CreateJwtTokenService();
        jwtTokenService.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<User>(),
                It.IsAny<ApplicationEntity>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<string, NotFoundError, FileNotFoundError>)"access_token");

        var refreshTokenService = Helpers.MockFactory.CreateRefreshTokenService();
        var service = CreateService(dbContext, passwordHasher, jwtTokenService, refreshTokenService);

        var request = new RegisterRequestDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "password123"
        };

        // Act
        var result = await service.RegisterAsync(app.Key, request, requiresSelfRegistrationEnabled: false);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var authResult = result.AsT0;
        await Assert.That(authResult.AccessToken.Length > 0).IsTrue();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "newuser");
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.Email).IsEqualTo("newuser@example.com");
    }

    [Test]
    public async Task RegisterAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var request = new RegisterRequestDto
        {
            Username = "user",
            Email = "user@example.com",
            Password = "password"
        };

        // Act
        var result = await service.RegisterAsync("nonexistent", request, requiresSelfRegistrationEnabled: false);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message!.Contains("not found")).IsTrue();
    }

    [Test]
    public async Task RegisterAsync_ShouldReturnInvalidOperationError_WhenSelfRegistrationIsDisabled()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(selfRegistrationMode: SelfRegistrationMode.Disabled);
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new RegisterRequestDto
        {
            Username = "user",
            Email = "user@example.com",
            Password = "password"
        };

        // Act - requiresSelfRegistrationEnabled should be TRUE to trigger the check
        var result = await service.RegisterAsync(app.Key, request, requiresSelfRegistrationEnabled: true);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("not enabled")).IsTrue();
    }

    [Test]
    public async Task RegisterAsync_ShouldAssignDefaultRole_WhenApplicationHasDefaultRole()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(selfRegistrationMode: SelfRegistrationMode.Open);
        var role = TestDataBuilder.CreateRole(app.Id, name: "DefaultRole");
        app.DefaultRoleId = role.Id;
        dbContext.Applications.Add(app);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var jwtTokenService = Helpers.MockFactory.CreateJwtTokenService();
        jwtTokenService.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<User>(),
                It.IsAny<ApplicationEntity>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<string, NotFoundError, FileNotFoundError>)"access_token");

        var service = CreateService(dbContext, jwtTokenService: jwtTokenService);
        var request = new RegisterRequestDto
        {
            Username = "user",
            Email = "user@example.com",
            Password = "password"
        };

        // Act
        var result = await service.RegisterAsync(app.Key, request, requiresSelfRegistrationEnabled: false);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "user");
        await Assert.That(user).IsNotNull();

        var userRole = await dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user!.Id && ur.RoleId == role.Id);
        await Assert.That(userRole).IsNotNull();
    }

    [Test]
    public async Task LoginAsync_ShouldReturnAuthResult_WhenCredentialsAreValid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var passwordHasher = Helpers.MockFactory.CreatePasswordHasher();
        var user = TestDataBuilder.CreateUser(userName: "testuser", email: "test@example.com", passwordHash: "hashed_password123");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var jwtTokenService = Helpers.MockFactory.CreateJwtTokenService();
        jwtTokenService.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<User>(),
                It.IsAny<ApplicationEntity>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<string, NotFoundError, FileNotFoundError>)"access_token");

        var accountLockoutService = new Mock<IAccountLockoutService>();
        accountLockoutService.Setup(x => x.IsAccountLockedAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        accountLockoutService.Setup(x => x.ResetFailedLoginAttemptsAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, passwordHasher, jwtTokenService, accountLockoutService: accountLockoutService);

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "password123",
            AppKey = app.Key
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var authResult = result.AsT0;
        await Assert.That(authResult.AccessToken.Length > 0).IsTrue();
    }

    [Test]
    public async Task LoginAsync_ShouldReturnUnauthorizedError_WhenPasswordIsInvalid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var passwordHasher = Helpers.MockFactory.CreatePasswordHasher();
        var user = TestDataBuilder.CreateUser(userName: "testuser", passwordHash: "hashed_wrong");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var accountLockoutService = new Mock<IAccountLockoutService>();
        accountLockoutService.Setup(x => x.IsAccountLockedAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        accountLockoutService.Setup(x => x.RecordFailedLoginAttemptAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, passwordHasher, accountLockoutService: accountLockoutService);

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "wrongpassword",
            AppKey = app.Key
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("Invalid credentials")).IsTrue();

        accountLockoutService.Verify(x => x.RecordFailedLoginAttemptAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoginAsync_ShouldReturnUnauthorizedError_WhenAccountIsLocked()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var user = TestDataBuilder.CreateUser(userName: "testuser", passwordHash: "hashed_password123");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var accountLockoutService = new Mock<IAccountLockoutService>();
        accountLockoutService.Setup(x => x.IsAccountLockedAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(dbContext, accountLockoutService: accountLockoutService);

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "password123",
            AppKey = app.Key
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("locked")).IsTrue();
    }

    [Test]
    public async Task LoginAsync_ShouldReturnUnauthorizedError_WhenUserDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "nonexistent",
            Password = "password",
            AppKey = app.Key
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("Invalid credentials")).IsTrue();
    }

    [Test]
    public async Task LoginAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "user",
            Password = "password",
            AppKey = "nonexistent"
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message!.Contains("not found")).IsTrue();
    }

    [Test]
    public async Task RefreshTokenAsync_ShouldReturnAuthResult_WhenTokenIsValid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var refreshToken = TestDataBuilder.CreateRefreshToken(user.Id, app.Id, token: "valid_token");
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync();

        var refreshTokenService = Helpers.MockFactory.CreateRefreshTokenService();
        refreshTokenService.Setup(x => x.ValidateRefreshTokenAsync("valid_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<RefreshToken, UnauthorizedError>)refreshToken);

        var jwtTokenService = Helpers.MockFactory.CreateJwtTokenService();
        jwtTokenService.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<User>(),
                It.IsAny<ApplicationEntity>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<string, NotFoundError, FileNotFoundError>)"new_access_token");

        var service = CreateService(dbContext, jwtTokenService: jwtTokenService, refreshTokenService: refreshTokenService);

        // Act
        var result = await service.RefreshTokenAsync("valid_token");

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var authResult = result.AsT0;
        await Assert.That(authResult.AccessToken.Length > 0).IsTrue();
    }

    [Test]
    public async Task RefreshTokenAsync_ShouldReturnUnauthorizedError_WhenTokenIsInvalid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var refreshTokenService = Helpers.MockFactory.CreateRefreshTokenService();
        refreshTokenService.Setup(x => x.ValidateRefreshTokenAsync("invalid_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<RefreshToken, UnauthorizedError>)new UnauthorizedError("Invalid token"));

        var service = CreateService(dbContext, refreshTokenService: refreshTokenService);

        // Act
        var result = await service.RefreshTokenAsync("invalid_token");

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message.Contains("Invalid")).IsTrue();
    }

    [Test]
    public async Task RevokeRefreshTokenAsync_ShouldReturnSuccess_WhenTokenExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var refreshTokenService = Helpers.MockFactory.CreateRefreshTokenService();
        refreshTokenService.Setup(x => x.RevokeRefreshTokenAsync("token", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, refreshTokenService: refreshTokenService);

        // Act
        var result = await service.RevokeRefreshTokenAsync("token");

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        refreshTokenService.Verify(x => x.RevokeRefreshTokenAsync("token", It.IsAny<CancellationToken>()), Times.Once);
    }

    #region Email Verification Tests

    [Test]
    public async Task RegisterAsync_ShouldSendVerificationEmail_WhenNewUserCreated()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(selfRegistrationMode: SelfRegistrationMode.Open);
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var passwordHasher = Helpers.MockFactory.CreatePasswordHasher();
        var jwtTokenService = Helpers.MockFactory.CreateJwtTokenService();
        jwtTokenService.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<User>(),
                It.IsAny<ApplicationEntity>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<string, NotFoundError, FileNotFoundError>)"access_token");

        var refreshTokenService = Helpers.MockFactory.CreateRefreshTokenService();

        var emailVerificationService = new Mock<IEmailVerificationService>();
        emailVerificationService.Setup(x => x.SendVerificationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<Contracts.Auth.EmailVerificationResponseDto, NotFoundError>)
                new Contracts.Auth.EmailVerificationResponseDto
                {
                    Message = "Verification email sent",
                    IsVerified = false
                });

        var service = CreateService(dbContext, passwordHasher, jwtTokenService, refreshTokenService,
            emailVerificationService: emailVerificationService);

        var request = new RegisterRequestDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "password123"
        };

        // Act
        var result = await service.RegisterAsync(app.Key, request, requiresSelfRegistrationEnabled: false);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "newuser");
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.EmailVerified).IsFalse();

        // Give background task time to execute
        await Task.Delay(100);

        // Verify email service was called (may be called in background)
        emailVerificationService.Verify(x => x.SendVerificationEmailAsync(
            "newuser@example.com",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task LoginAsync_ShouldReturnUnauthorizedError_WhenEmailNotVerifiedAndRequired()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        app.RequireEmailVerification = true;
        dbContext.Applications.Add(app);

        var passwordHasher = Helpers.MockFactory.CreatePasswordHasher();
        var user = TestDataBuilder.CreateUser(userName: "testuser", email: "test@example.com", passwordHash: "hashed_password123");
        user.EmailVerified = false;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var accountLockoutService = new Mock<IAccountLockoutService>();
        accountLockoutService.Setup(x => x.IsAccountLockedAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService(dbContext, passwordHasher, accountLockoutService: accountLockoutService);

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "password123",
            AppKey = app.Key
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("not been verified")).IsTrue();
    }

    [Test]
    public async Task LoginAsync_ShouldSucceed_WhenEmailIsVerified()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        app.RequireEmailVerification = true;
        dbContext.Applications.Add(app);

        var passwordHasher = Helpers.MockFactory.CreatePasswordHasher();
        var user = TestDataBuilder.CreateUser(userName: "testuser", email: "test@example.com", passwordHash: "hashed_password123");
        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTimeOffset.UtcNow;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var jwtTokenService = Helpers.MockFactory.CreateJwtTokenService();
        jwtTokenService.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<User>(),
                It.IsAny<ApplicationEntity>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<string, NotFoundError, FileNotFoundError>)"access_token");

        var accountLockoutService = new Mock<IAccountLockoutService>();
        accountLockoutService.Setup(x => x.IsAccountLockedAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        accountLockoutService.Setup(x => x.ResetFailedLoginAttemptsAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, passwordHasher, jwtTokenService, accountLockoutService: accountLockoutService);

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "password123",
            AppKey = app.Key
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var authResult = result.AsT0;
        await Assert.That(authResult.AccessToken.Length > 0).IsTrue();
    }

    [Test]
    public async Task LoginAsync_ShouldSucceed_WhenEmailNotVerifiedButNotRequired()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        app.RequireEmailVerification = false;
        dbContext.Applications.Add(app);

        var passwordHasher = Helpers.MockFactory.CreatePasswordHasher();
        var user = TestDataBuilder.CreateUser(userName: "testuser", email: "test@example.com", passwordHash: "hashed_password123");
        user.EmailVerified = false;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var jwtTokenService = Helpers.MockFactory.CreateJwtTokenService();
        jwtTokenService.Setup(x => x.GenerateAccessTokenAsync(
                It.IsAny<User>(),
                It.IsAny<ApplicationEntity>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OneOf<string, NotFoundError, FileNotFoundError>)"access_token");

        var accountLockoutService = new Mock<IAccountLockoutService>();
        accountLockoutService.Setup(x => x.IsAccountLockedAsync(It.IsAny<User>(), It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        accountLockoutService.Setup(x => x.ResetFailedLoginAttemptsAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(dbContext, passwordHasher, jwtTokenService, accountLockoutService: accountLockoutService);

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "password123",
            AppKey = app.Key
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var authResult = result.AsT0;
        await Assert.That(authResult.AccessToken.Length > 0).IsTrue();
    }

    #endregion
}


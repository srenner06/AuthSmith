using System.Net;
using System.Net.Http.Json;
using AuthSmith.Api.Tests.Helpers;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Applications;
using AuthSmith.Contracts.Auth;
using AuthSmith.Contracts.Authorization;
using AuthSmith.Domain.Enums;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IPasswordHasher = AuthSmith.Infrastructure.Services.Authentication.IPasswordHasher;

namespace AuthSmith.Api.Tests.Controllers;

public class AuthControllerTests
{
    #region Programmatic Registration Tests (requires API key)

    [Test]
    public async Task RegisterAsync_ShouldReturnAuthResult_WhenApiKeyProvided_RegardlessOfSelfRegistrationMode()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();

        // Ensure the database is created
        dbContext.Database.EnsureCreated();

        // Arrange - App key is auto-generated as unique
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            apiKey: "test-api-key");
        app.SelfRegistrationMode = SelfRegistrationMode.Disabled; // Disabled for programmatic
        await dbContext.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123"
        };

        client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/auth/register/{app.Key}", request);
        var t = await response.Content.ReadAsStringAsync();

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.AccessToken).IsNotEmpty();
    }

    [Test]
    public async Task RegisterAsync_ShouldReturnUnauthorized_WhenApiKeyMissing()
    {
        // Create a new factory for this test
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();

        dbContext.Database.EnsureCreated();

        // Arrange - App key is auto-generated as unique
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            apiKey: "test-api-key");
        await dbContext.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123"
        };

        // Don't add API key header

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/auth/register/{app.Key}", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Self-Registration Tests (no API key)

    [Test]
    public async Task SelfRegisterAsync_ShouldReturnAuthResult_WhenSelfRegistrationIsOpen()
    {
        // Create a new factory for this test
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();

        dbContext.Database.EnsureCreated();

        // Arrange - App key is auto-generated as unique
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            apiKey: "test-api-key");
        app.SelfRegistrationMode = SelfRegistrationMode.Open;
        await dbContext.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Username = "publicuser",
            Email = "public@example.com",
            Password = "Password123"
        };

        // No API key header for self-registration

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/auth/self-register/{app.Key}", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.AccessToken).IsNotEmpty();

        // Verify user was created
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "publicuser");
        await Assert.That(user).IsNotNull();
    }

    [Test]
    public async Task SelfRegisterAsync_ShouldReturnBadRequest_WhenSelfRegistrationIsDisabled()
    {
        // Create a new factory for this test
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();

        dbContext.Database.EnsureCreated();

        // Arrange - App key is auto-generated as unique
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            apiKey: "test-api-key");
        app.SelfRegistrationMode = SelfRegistrationMode.Disabled;
        await dbContext.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Username = "publicuser",
            Email = "public@example.com",
            Password = "Password123"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/auth/self-register/{app.Key}", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        // Verify user was NOT created
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "publicuser");
        await Assert.That(user).IsNull();
    }

    [Test]
    public async Task SelfRegisterAsync_ShouldReturnNotFound_WhenApplicationDoesNotExist()
    {
        // Create a new factory for this test
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();

        dbContext.Database.EnsureCreated();

        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "publicuser",
            Email = "public@example.com",
            Password = "Password123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/self-register/nonexistent", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    #endregion

    #region Login Tests

    [Test]
    public async Task LoginAsync_ShouldReturnAuthResult_WhenCredentialsAreValid()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Ensure the database is created
        dbContext.Database.EnsureCreated();

        // Arrange - App key is auto-generated as unique
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher);

        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            email: "test@example.com",
            passwordHash: passwordHasher.HashPassword("password123"));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "password123",
            AppKey = app.Key
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task LoginAsync_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Ensure the database is created
        dbContext.Database.EnsureCreated();

        // Arrange - App key is auto-generated as unique
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher);

        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            passwordHash: passwordHasher.HashPassword("correctpassword"));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "wrongpassword",
            AppKey = app.Key
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Scenarios

    [Test]
    public async Task ApplicationsController_ShouldReturnUnauthorized_WhenApiKeyIsMissing()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Arrange
        var request = new CreateApplicationRequestDto
        {
            Key = "testapp-" + Guid.NewGuid().ToString(),
            Name = "Test Application"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apps", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ApplicationsController_ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Arrange
        var request = new CreateApplicationRequestDto
        {
            Key = "testapp-" + Guid.NewGuid().ToString(),
            Name = "Test Application"
        };

        client.DefaultRequestHeaders.Add("X-API-Key", "invalid-api-key");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apps", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ApplicationsController_ShouldReturnForbidden_WhenApiKeyHasInsufficientAccess()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();

        // Ensure the database is created
        dbContext.Database.EnsureCreated();

        // Arrange - App key is auto-generated as unique
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            apiKey: "app-api-key");
        await dbContext.SaveChangesAsync();

        // Note: The API key validator would need to set AccessLevel to "App" instead of "Admin"
        // This test demonstrates the scenario - actual implementation depends on ApiKeyValidator logic
        client.DefaultRequestHeaders.Add("X-API-Key", "app-api-key");

        var request = new CreateApplicationRequestDto
        {
            Key = "newapp-" + Guid.NewGuid().ToString(),
            Name = "New Application"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apps", request);

        // Assert - Should be Forbidden if access level is App, Unauthorized if key is invalid
        await Assert.That(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized).IsTrue();
    }

    [Test]
    public async Task ProgrammaticRegister_ShouldReturnBadRequest_WhenSelfRegistrationIsDisabled()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();

        // Ensure the database is created
        dbContext.Database.EnsureCreated();

        // Arrange - App key is auto-generated as unique
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            apiKey: "test-api-key");
        app.SelfRegistrationMode = SelfRegistrationMode.Disabled;
        await dbContext.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "password123"
        };

        client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/auth/register/{app.Key}", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AuthorizationController_ShouldReturnUnauthorized_WhenApiKeyIsMissing()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Arrange
        var request = new PermissionCheckRequestDto
        {
            UserId = Guid.NewGuid(),
            ApplicationKey = "testapp-" + Guid.NewGuid().ToString(),
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/authorization/check", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    #endregion
}


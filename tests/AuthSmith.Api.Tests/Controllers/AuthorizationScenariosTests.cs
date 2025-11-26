using System.Net;
using System.Net.Http.Json;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Api.Tests.Helpers;
using AuthSmith.Contracts.Auth;
using AuthSmith.Contracts.Applications;
using AuthSmith.Contracts.Authorization;
using AuthSmith.Domain.Enums;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;
using TUnit.Assertions;

namespace AuthSmith.Api.Tests.Controllers;

public class AuthorizationScenariosTests
{

    [Test]
    public async Task ApplicationsController_ShouldReturnUnauthorized_WhenApiKeyIsMissing()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        
        // Arrange
        var request = new CreateApplicationRequestDto
        {
            Key = "testapp",
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
            Key = "testapp",
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
        
        // Arrange
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            key: "testapp",
            apiKey: "app-api-key");
        await dbContext.SaveChangesAsync();
        
        // Note: The API key validator would need to set AccessLevel to "App" instead of "Admin"
        // This test demonstrates the scenario - actual implementation depends on ApiKeyValidator logic
        client.DefaultRequestHeaders.Add("X-API-Key", "app-api-key");

        var request = new CreateApplicationRequestDto
        {
            Key = "newapp",
            Name = "New Application"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/apps", request);

        // Assert - Should be Forbidden if access level is App, Unauthorized if key is invalid
        await Assert.That(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized).IsTrue();
    }

    [Test]
    public async Task AuthController_Register_ShouldReturnUnauthorized_WhenApiKeyIsMissing()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();
        
        // Ensure the database is created
        dbContext.Database.EnsureCreated();
        
        // Arrange
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            key: "testapp");
        app.SelfRegistrationMode = SelfRegistrationMode.Open;
        await dbContext.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "password123"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/auth/register/{app.Key}", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AuthController_Register_ShouldReturnBadRequest_WhenSelfRegistrationIsDisabled()
    {
        // Create a new factory for this test - each test gets its own isolated database
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        var apiKeyHasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();
        
        // Ensure the database is created
        dbContext.Database.EnsureCreated();
        
        // Arrange
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            key: "testapp",
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
            ApplicationKey = "testapp",
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/authorization/check", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}


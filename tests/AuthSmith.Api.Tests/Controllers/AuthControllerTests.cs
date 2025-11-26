using System.Net;
using System.Net.Http.Json;
using AuthSmith.Api.Tests.Helpers;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Auth;
using AuthSmith.Domain.Enums;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IPasswordHasher = AuthSmith.Infrastructure.Services.Authentication.IPasswordHasher;

namespace AuthSmith.Api.Tests.Controllers;

public class AuthControllerTests
{

    [Test]
    public async Task RegisterAsync_ShouldReturnAuthResult_WhenRegistrationIsSuccessful()
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
        app.SelfRegistrationMode = SelfRegistrationMode.Open;
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

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsNotEmpty();
    }

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

        // Arrange
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            key: "testapp");

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
            AppKey = "testapp"
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

        // Arrange
        var app = await TestHelpers.CreateApplicationWithApiKeyAsync(
            dbContext,
            apiKeyHasher,
            key: "testapp");

        var user = TestDataBuilder.CreateUser(
            userName: "testuser",
            passwordHash: passwordHasher.HashPassword("correctpassword"));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            UsernameOrEmail = "testuser",
            Password = "wrongpassword",
            AppKey = "testapp"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}


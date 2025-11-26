using AuthSmith.Application.Services.Applications;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Applications;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Enums;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TUnit.Core;
using TUnit.Assertions;

namespace AuthSmith.Application.Tests.Services.Applications;

public class ApplicationServiceTests : TestBase
{
    private static AuthSmithDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthSmithDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AuthSmithDbContext(options);
    }

    private ApplicationService CreateService(
        AuthSmithDbContext? dbContext = null,
        Mock<Infrastructure.Services.Authentication.IApiKeyHasher>? apiKeyHasher = null,
        Mock<Infrastructure.Services.Caching.IPermissionCache>? permissionCache = null,
        Mock<ILogger<ApplicationService>>? logger = null)
    {
        dbContext ??= CreateDbContext();
        apiKeyHasher ??= Helpers.MockFactory.CreateApiKeyHasher();
        permissionCache ??= Helpers.MockFactory.CreatePermissionCache();
        logger ??= CreateLoggerMock<ApplicationService>();

        return new ApplicationService(
            dbContext,
            apiKeyHasher.Object,
            permissionCache.Object,
            logger.Object);
    }

    [Test]
    public async Task CreateAsync_ShouldCreateApplication_WhenRequestIsValid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var request = new CreateApplicationRequestDto
        {
            Key = "testapp",
            Name = "Test Application",
            SelfRegistrationMode = SelfRegistrationMode.Open,
            AccountLockoutEnabled = true,
            MaxFailedLoginAttempts = 5,
            LockoutDurationMinutes = 15
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var application = result.AsT0;
        await Assert.That(application.Key).IsEqualTo("testapp");
        await Assert.That(application.Name).IsEqualTo("Test Application");
        await Assert.That(application.IsActive).IsTrue();

        var dbApplication = await dbContext.Applications.FirstOrDefaultAsync(a => a.Id == application.Id);
        await Assert.That(dbApplication).IsNotNull();
        await Assert.That(dbApplication!.Key).IsEqualTo("testapp");
    }

    [Test]
    public async Task CreateAsync_ShouldReturnConflictError_WhenKeyAlreadyExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var existingApp = TestDataBuilder.CreateApplication(key: "testapp");
        dbContext.Applications.Add(existingApp);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new CreateApplicationRequestDto
        {
            Key = "testapp",
            Name = "Another Application"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message.Contains("already exists")).IsTrue();
    }

    [Test]
    public async Task CreateAsync_ShouldBeCaseInsensitive_WhenCheckingKey()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var existingApp = TestDataBuilder.CreateApplication(key: "testapp");
        dbContext.Applications.Add(existingApp);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new CreateApplicationRequestDto
        {
            Key = "TESTAPP",
            Name = "Another Application"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
    }

    [Test]
    public async Task ListAsync_ShouldReturnEmptyList_WhenNoApplicationsExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        // Act
        var result = await service.ListAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ListAsync_ShouldReturnAllApplications_OrderedByKey()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app1 = TestDataBuilder.CreateApplication(key: "zapp", name: "Z App");
        var app2 = TestDataBuilder.CreateApplication(key: "aapp", name: "A App");
        dbContext.Applications.AddRange(app1, app2);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.ListAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Key).IsEqualTo("aapp");
        await Assert.That(result[1].Key).IsEqualTo("zapp");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnApplication_WhenExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp", name: "Test App");
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetByIdAsync(app.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var application = result.AsT0;
        await Assert.That(application.Id).IsEqualTo(app.Id);
        await Assert.That(application.Key).IsEqualTo("testapp");
        await Assert.That(application.Name).IsEqualTo("Test App");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetByIdAsync(nonExistentId);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Application not found.");
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateApplication_WhenRequestIsValid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp", name: "Old Name");
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache: permissionCache);
        var request = new UpdateApplicationRequestDto
        {
            Name = "New Name",
            IsActive = false
        };

        // Act
        var result = await service.UpdateAsync(app.Id, request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var application = result.AsT0;
        await Assert.That(application.Name).IsEqualTo("New Name");
        await Assert.That(application.IsActive).IsFalse();

        permissionCache.Verify(x => x.InvalidateApplicationPermissionsAsync(app.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateApplicationRequestDto { Name = "New Name" };

        // Act
        var result = await service.UpdateAsync(nonExistentId, request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Application not found.");
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnNotFoundError_WhenDefaultRoleDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new UpdateApplicationRequestDto
        {
            DefaultRoleId = Guid.NewGuid()
        };

        // Act
        var result = await service.UpdateAsync(app.Id, request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message!.Contains("Role not found")).IsTrue();
    }

    [Test]
    public async Task UpdateAsync_ShouldReturnNotFoundError_WhenDefaultRoleBelongsToDifferentApplication()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app1 = TestDataBuilder.CreateApplication(key: "app1");
        var app2 = TestDataBuilder.CreateApplication(key: "app2");
        dbContext.Applications.AddRange(app1, app2);
        await dbContext.SaveChangesAsync();

        var role = TestDataBuilder.CreateRole(app2.Id, name: "Role");
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new UpdateApplicationRequestDto
        {
            DefaultRoleId = role.Id
        };

        // Act
        var result = await service.UpdateAsync(app1.Id, request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message!.Contains("Role not found")).IsTrue();
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateDefaultRole_WhenValid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var role = TestDataBuilder.CreateRole(app.Id, name: "DefaultRole");
        dbContext.Applications.Add(app);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache: permissionCache);
        var request = new UpdateApplicationRequestDto
        {
            DefaultRoleId = role.Id
        };

        // Act
        var result = await service.UpdateAsync(app.Id, request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var application = result.AsT0;
        await Assert.That(application.DefaultRoleId).IsEqualTo(role.Id);
    }

    [Test]
    public async Task GenerateApiKeyAsync_ShouldGenerateApiKey_WhenApplicationExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var apiKeyHasher = Helpers.MockFactory.CreateApiKeyHasher();
        var service = CreateService(dbContext, apiKeyHasher: apiKeyHasher);

        // Act
        var result = await service.GenerateApiKeyAsync(app.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var apiKey = result.AsT0;
        await Assert.That(apiKey).IsNotNull();
        await Assert.That(apiKey.Length > 0).IsTrue();

        apiKeyHasher.Verify(x => x.HashApiKey(It.IsAny<string>()), Times.Once);

        var dbApp = await dbContext.Applications.FindAsync([app.Id]);
        await Assert.That(dbApp!.ApiKeyHash).IsNotNull();
    }

    [Test]
    public async Task GenerateApiKeyAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GenerateApiKeyAsync(nonExistentId);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Application not found.");
    }
}


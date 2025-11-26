using AuthSmith.Application.Services.Permissions;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Permissions;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TUnit.Assertions;
using TUnit.Core;

namespace AuthSmith.Application.Tests.Services.Permissions;

public class PermissionServiceTests : TestBase
{
    private static AuthSmithDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthSmithDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AuthSmithDbContext(options);
    }

    private PermissionService CreateService(
        AuthSmithDbContext? dbContext = null,
        Mock<Infrastructure.Services.Caching.IPermissionCache>? permissionCache = null,
        Mock<ILogger<PermissionService>>? logger = null)
    {
        dbContext ??= CreateDbContext();
        permissionCache ??= Helpers.MockFactory.CreatePermissionCache();
        logger ??= CreateLoggerMock<PermissionService>();

        return new PermissionService(
            dbContext,
            permissionCache.Object,
            logger.Object);
    }

    [Test]
    public async Task CreateAsync_ShouldCreatePermission_WhenRequestIsValid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new CreatePermissionRequestDto
        {
            Module = "Catalog",
            Action = "Read",
            Description = "Read catalog items"
        };

        // Act
        var result = await service.CreateAsync(app.Id, request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var permission = result.AsT0;
        await Assert.That(permission.Module).IsEqualTo("Catalog");
        await Assert.That(permission.Action).IsEqualTo("Read");
        await Assert.That(permission.Code).IsEqualTo("testapp.catalog.read");
        await Assert.That(permission.ApplicationId).IsEqualTo(app.Id);

        var dbPermission = await dbContext.Permissions.FirstOrDefaultAsync(p => p.Id == permission.Id);
        await Assert.That(dbPermission).IsNotNull();
    }

    [Test]
    public async Task CreateAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var request = new CreatePermissionRequestDto
        {
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var result = await service.CreateAsync(Guid.NewGuid(), request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Application not found.");
    }

    [Test]
    public async Task CreateAsync_ShouldReturnConflictError_WhenPermissionCodeAlreadyExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var existingPermission = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read", code: "testapp.catalog.read");
        dbContext.Applications.Add(app);
        dbContext.Permissions.Add(existingPermission);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new CreatePermissionRequestDto
        {
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var result = await service.CreateAsync(app.Id, request);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("already exists")).IsTrue();
    }

    [Test]
    public async Task ListAsync_ShouldReturnAllPermissions_WhenApplicationExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var permission1 = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read");
        var permission2 = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Write");
        dbContext.Applications.Add(app);
        dbContext.Permissions.AddRange(permission1, permission2);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.ListAsync(app.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var permissions = result.AsT0;
        await Assert.That(permissions.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ListAsync_ShouldReturnEmptyList_WhenNoPermissionsExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.ListAsync(app.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var permissions = result.AsT0;
        await Assert.That(permissions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnPermission_WhenExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read");
        dbContext.Applications.Add(app);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetByIdAsync(app.Id, permission.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var permissionDto = result.AsT0;
        await Assert.That(permissionDto.Id).IsEqualTo(permission.Id);
        await Assert.That(permissionDto.Module).IsEqualTo("Catalog");
        await Assert.That(permissionDto.Action).IsEqualTo("Read");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNotFoundError_WhenPermissionDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetByIdAsync(app.Id, Guid.NewGuid());

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Permission not found.");
    }

    [Test]
    public async Task DeleteAsync_ShouldDeletePermission_WhenExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read");
        dbContext.Applications.Add(app);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.DeleteAsync(app.Id, permission.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var deletedPermission = await dbContext.Permissions.FindAsync([permission.Id]);
        await Assert.That(deletedPermission).IsNull();

        permissionCache.Verify(x => x.InvalidateApplicationPermissionsAsync(app.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ShouldReturnNotFoundError_WhenPermissionDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.DeleteAsync(app.Id, Guid.NewGuid());

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Permission not found.");
    }
}


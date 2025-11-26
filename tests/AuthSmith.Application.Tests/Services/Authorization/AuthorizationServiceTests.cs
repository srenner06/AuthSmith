using AuthSmith.Application.Services.Authorization;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Authorization;
using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthSmith.Application.Tests.Services.Authorization;

public class AuthorizationServiceTests : TestBase
{
    private static AuthSmithDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthSmithDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AuthSmithDbContext(options);
    }

    private AuthorizationService CreateService(
        AuthSmithDbContext? dbContext = null,
        Mock<Infrastructure.Services.Caching.IPermissionCache>? permissionCache = null,
        Mock<ILogger<AuthorizationService>>? logger = null)
    {
        dbContext ??= CreateDbContext();
        permissionCache ??= Helpers.MockFactory.CreatePermissionCache();
        logger ??= CreateLoggerMock<AuthorizationService>();

        return new AuthorizationService(
            dbContext,
            permissionCache.Object,
            logger.Object);
    }

    [Test]
    public async Task CheckPermissionAsync_ShouldReturnTrue_WhenUserHasPermissionViaRole()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read", code: "testapp.catalog.read");
        var userRole = TestDataBuilder.CreateUserRole(user.Id, role.Id);
        var rolePermission = TestDataBuilder.CreateRolePermission(role.Id, permission.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        dbContext.UserRoles.Add(userRole);
        dbContext.RolePermissions.Add(rolePermission);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        permissionCache.Setup(x => x.GetUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HashSet<string>?)null);

        var service = CreateService(dbContext, permissionCache);
        var request = new PermissionCheckRequestDto
        {
            UserId = user.Id,
            ApplicationKey = "testapp",
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var result = await service.CheckPermissionAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var checkResult = result.AsT0;
        await Assert.That(checkResult.HasPermission).IsTrue();
        await Assert.That(checkResult.Source).IsEqualTo("Role");

        permissionCache.Verify(x => x.SetUserPermissionsAsync(user.Id, app.Id, It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CheckPermissionAsync_ShouldReturnTrue_WhenUserHasDirectPermission()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read", code: "testapp.catalog.read");
        var userPermission = TestDataBuilder.CreateUserPermission(user.Id, permission.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Permissions.Add(permission);
        dbContext.UserPermissions.Add(userPermission);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        permissionCache.Setup(x => x.GetUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HashSet<string>?)null);

        var service = CreateService(dbContext, permissionCache);
        var request = new PermissionCheckRequestDto
        {
            UserId = user.Id,
            ApplicationKey = "testapp",
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var result = await service.CheckPermissionAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var checkResult = result.AsT0;
        await Assert.That(checkResult.HasPermission).IsTrue();
        await Assert.That(checkResult.Source).IsEqualTo("Direct");
    }

    [Test]
    public async Task CheckPermissionAsync_ShouldReturnFalse_WhenUserDoesNotHavePermission()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        permissionCache.Setup(x => x.GetUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HashSet<string>?)null);

        var service = CreateService(dbContext, permissionCache);
        var request = new PermissionCheckRequestDto
        {
            UserId = user.Id,
            ApplicationKey = "testapp",
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var result = await service.CheckPermissionAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var checkResult = result.AsT0;
        await Assert.That(checkResult.HasPermission).IsFalse();
        await Assert.That(checkResult.Source).IsEqualTo("None");
    }

    [Test]
    public async Task CheckPermissionAsync_ShouldReturnTrueFromCache_WhenPermissionIsCached()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var cachedPermissions = new HashSet<string> { "testapp.catalog.read" };
        permissionCache.Setup(x => x.GetUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPermissions);

        var service = CreateService(dbContext, permissionCache);
        var request = new PermissionCheckRequestDto
        {
            UserId = user.Id,
            ApplicationKey = "testapp",
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var result = await service.CheckPermissionAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var checkResult = result.AsT0;
        await Assert.That(checkResult.HasPermission).IsTrue();
        await Assert.That(checkResult.Source).IsEqualTo("Cache");
    }

    [Test]
    public async Task CheckPermissionAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new PermissionCheckRequestDto
        {
            UserId = user.Id,
            ApplicationKey = "nonexistent",
            Module = "Catalog",
            Action = "Read"
        };

        // Act
        var result = await service.CheckPermissionAsync(request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message!.Contains("not found")).IsTrue();
    }

    [Test]
    public async Task BulkCheckPermissionsAsync_ShouldReturnResults_WhenApplicationExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var permission1 = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read", code: "testapp.catalog.read");
        var permission2 = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Write", code: "testapp.catalog.write");
        var userRole = TestDataBuilder.CreateUserRole(user.Id, role.Id);
        var rolePermission = TestDataBuilder.CreateRolePermission(role.Id, permission1.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.AddRange(permission1, permission2);
        dbContext.UserRoles.Add(userRole);
        dbContext.RolePermissions.Add(rolePermission);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new BulkPermissionCheckRequestDto
        {
            UserId = user.Id,
            ApplicationKey = "testapp",
            Checks = new List<PermissionCheckItemDto>
            {
                new() { Module = "Catalog", Action = "Read" },
                new() { Module = "Catalog", Action = "Write" }
            }
        };

        // Act
        var result = await service.BulkCheckPermissionsAsync(request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var bulkResult = result.AsT0;
        await Assert.That(bulkResult.Results.Count).IsEqualTo(2);
        await Assert.That(bulkResult.Results[0].HasPermission).IsTrue();
        await Assert.That(bulkResult.Results[1].HasPermission).IsFalse();
    }

    [Test]
    public async Task GetUserPermissionsAsync_ShouldReturnAllPermissions_WhenUserHasPermissions()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var permission1 = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read", code: "testapp.catalog.read");
        var permission2 = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Write", code: "testapp.catalog.write");
        var userRole = TestDataBuilder.CreateUserRole(user.Id, role.Id);
        var rolePermission = TestDataBuilder.CreateRolePermission(role.Id, permission1.Id);
        var userPermission = TestDataBuilder.CreateUserPermission(user.Id, permission2.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.AddRange(permission1, permission2);
        dbContext.UserRoles.Add(userRole);
        dbContext.RolePermissions.Add(rolePermission);
        dbContext.UserPermissions.Add(userPermission);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetUserPermissionsAsync(user.Id, "testapp");

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var permissions = result.AsT0;
        await Assert.That(permissions.Count).IsEqualTo(2);
        await Assert.That(permissions.Contains("testapp.catalog.read")).IsTrue();
        await Assert.That(permissions.Contains("testapp.catalog.write")).IsTrue();
    }

    [Test]
    public async Task GetUserPermissionsAsync_ShouldReturnFilteredPermissions_WhenModuleNameIsProvided()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication(key: "testapp");
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var permission1 = TestDataBuilder.CreatePermission(app.Id, module: "Catalog", action: "Read", code: "testapp.catalog.read");
        var permission2 = TestDataBuilder.CreatePermission(app.Id, module: "Orders", action: "Read", code: "testapp.orders.read");
        var userRole = TestDataBuilder.CreateUserRole(user.Id, role.Id);
        var rolePermission1 = TestDataBuilder.CreateRolePermission(role.Id, permission1.Id);
        var rolePermission2 = TestDataBuilder.CreateRolePermission(role.Id, permission2.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.AddRange(permission1, permission2);
        dbContext.UserRoles.Add(userRole);
        dbContext.RolePermissions.AddRange(rolePermission1, rolePermission2);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetUserPermissionsAsync(user.Id, "testapp", "Catalog");

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var permissions = result.AsT0;
        await Assert.That(permissions.Count).IsEqualTo(1);
        await Assert.That(permissions.Contains("testapp.catalog.read")).IsTrue();
    }

    [Test]
    public async Task GetUserPermissionsAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetUserPermissionsAsync(user.Id, "nonexistent");

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message!.Contains("not found")).IsTrue();
    }
}


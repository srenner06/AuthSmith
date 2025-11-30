using AuthSmith.Application.Services.Users;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthSmith.Application.Tests.Services.Users;

public class UserServiceTests : TestBase
{
    private static AuthSmithDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthSmithDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AuthSmithDbContext(options);
    }

    private UserService CreateService(
        AuthSmithDbContext? dbContext = null,
        Mock<Infrastructure.Services.Caching.IPermissionCache>? permissionCache = null,
        Mock<ILogger<UserService>>? logger = null)
    {
        dbContext ??= CreateDbContext();
        permissionCache ??= Helpers.MockFactory.CreatePermissionCache();
        logger ??= CreateLoggerMock<UserService>();

        return new UserService(
            dbContext,
            permissionCache.Object,
            logger.Object);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(userName: "testuser", email: "test@example.com");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetByIdAsync(user.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var userDto = result.AsT0;
        await Assert.That(userDto.Id).IsEqualTo(user.Id);
        await Assert.That(userDto.UserName).IsEqualTo("testuser");
        await Assert.That(userDto.Email).IsEqualTo("test@example.com");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNotFoundError_WhenUserDoesNotExist()
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
        await Assert.That(error.Message).IsEqualTo("User not found.");
    }

    [Test]
    public async Task SearchAsync_ShouldReturnAllUsers_WhenQueryIsEmpty()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user1 = TestDataBuilder.CreateUser(userName: "user1");
        var user2 = TestDataBuilder.CreateUser(userName: "user2");
        dbContext.Users.AddRange(user1, user2);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.SearchAsync(null);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task SearchAsync_ShouldReturnFilteredUsers_WhenQueryIsProvided()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user1 = TestDataBuilder.CreateUser(userName: "john", email: "john@example.com");
        var user2 = TestDataBuilder.CreateUser(userName: "jane", email: "jane@example.com");
        var user3 = TestDataBuilder.CreateUser(userName: "bob", email: "bob@example.com");
        dbContext.Users.AddRange(user1, user2, user3);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.SearchAsync("john");

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].UserName).IsEqualTo("john");
    }

    [Test]
    public async Task SearchAsync_ShouldReturnEmptyList_WhenNoMatches()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.SearchAsync("nonexistent");

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AssignRoleAsync_ShouldAssignRole_WhenUserAndRoleExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.AssignRoleAsync(user.Id, role.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var userRole = await dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        await Assert.That(userRole).IsNotNull();

        permissionCache.Verify(x => x.InvalidateUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AssignRoleAsync_ShouldReturnNotFoundError_WhenUserDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var role = TestDataBuilder.CreateRole(Guid.NewGuid(), name: "Admin");
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.AssignRoleAsync(Guid.NewGuid(), role.Id);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("User not found.");
    }

    [Test]
    public async Task AssignRoleAsync_ShouldReturnNotFoundError_WhenRoleDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.AssignRoleAsync(user.Id, Guid.NewGuid());

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Role not found.");
    }

    [Test]
    public async Task AssignRoleAsync_ShouldReturnConflictError_WhenRoleAlreadyAssigned()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var userRole = TestDataBuilder.CreateUserRole(user.Id, role.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(userRole);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.AssignRoleAsync(user.Id, role.Id);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("already has this role")).IsTrue();
    }

    [Test]
    public async Task RemoveRoleAsync_ShouldRemoveRole_WhenUserRoleExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var userRole = TestDataBuilder.CreateUserRole(user.Id, role.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(userRole);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.RemoveRoleAsync(user.Id, role.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var removedUserRole = await dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        await Assert.That(removedUserRole).IsNull();

        permissionCache.Verify(x => x.InvalidateUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemoveRoleAsync_ShouldReturnNotFoundError_WhenUserRoleDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(Guid.NewGuid(), name: "Admin");
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.RemoveRoleAsync(user.Id, role.Id);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("User role not found.");
    }

    [Test]
    public async Task AssignPermissionAsync_ShouldAssignPermission_WhenUserAndPermissionExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Test", action: "Read");
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.AssignPermissionAsync(user.Id, permission.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var userPermission = await dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == permission.Id);
        await Assert.That(userPermission).IsNotNull();

        permissionCache.Verify(x => x.InvalidateUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AssignPermissionAsync_ShouldReturnConflictError_WhenPermissionAlreadyAssigned()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Test", action: "Read");
        var userPermission = TestDataBuilder.CreateUserPermission(user.Id, permission.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Permissions.Add(permission);
        dbContext.UserPermissions.Add(userPermission);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.AssignPermissionAsync(user.Id, permission.Id);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("already has this permission")).IsTrue();
    }

    [Test]
    public async Task RemovePermissionAsync_ShouldRemovePermission_WhenUserPermissionExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Test", action: "Read");
        var userPermission = TestDataBuilder.CreateUserPermission(user.Id, permission.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Permissions.Add(permission);
        dbContext.UserPermissions.Add(userPermission);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.RemovePermissionAsync(user.Id, permission.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var removedUserPermission = await dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == user.Id && up.PermissionId == permission.Id);
        await Assert.That(removedUserPermission).IsNull();

        permissionCache.Verify(x => x.InvalidateUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RevokeAllAssignmentsAsync_ShouldRemoveAllRolesAndPermissions_WhenUserAndAppExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Test", action: "Read");
        var userRole = TestDataBuilder.CreateUserRole(user.Id, role.Id);
        var userPermission = TestDataBuilder.CreateUserPermission(user.Id, permission.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        dbContext.UserRoles.Add(userRole);
        dbContext.UserPermissions.Add(userPermission);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.RevokeAllAssignmentsAsync(user.Id, app.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var remainingUserRoles = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id && ur.RoleId == role.Id)
            .ToListAsync();
        await Assert.That(remainingUserRoles.Count).IsEqualTo(0);

        var remainingUserPermissions = await dbContext.UserPermissions
            .Where(up => up.UserId == user.Id && up.PermissionId == permission.Id)
            .ToListAsync();
        await Assert.That(remainingUserPermissions.Count).IsEqualTo(0);

        permissionCache.Verify(x => x.InvalidateUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}


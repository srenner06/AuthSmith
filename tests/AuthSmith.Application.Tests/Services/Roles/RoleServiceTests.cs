using AuthSmith.Application.Services.Roles;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Contracts.Roles;
using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthSmith.Application.Tests.Services.Roles;

public class RoleServiceTests : TestBase
{
    private static AuthSmithDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthSmithDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AuthSmithDbContext(options);
    }

    private RoleService CreateService(
        AuthSmithDbContext? dbContext = null,
        Mock<Infrastructure.Services.Caching.IPermissionCache>? permissionCache = null,
        Mock<ILogger<RoleService>>? logger = null)
    {
        dbContext ??= CreateDbContext();
        permissionCache ??= Helpers.MockFactory.CreatePermissionCache();
        var auditService = Helpers.MockFactory.CreateAuditService();
        var requestContext = Helpers.MockFactory.CreateRequestContextService();
        logger ??= CreateLoggerMock<RoleService>();

        return new RoleService(
            dbContext,
            permissionCache.Object,
            auditService.Object,
            requestContext.Object,
            logger.Object);
    }

    [Test]
    public async Task CreateAsync_ShouldCreateRole_WhenRequestIsValid()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new CreateRoleRequestDto
        {
            Name = "Admin",
            Description = "Administrator role"
        };

        // Act
        var result = await service.CreateAsync(app.Id, request);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var role = result.AsT0;
        await Assert.That(role.Name).IsEqualTo("Admin");
        await Assert.That(role.Description).IsEqualTo("Administrator role");
        await Assert.That(role.ApplicationId).IsEqualTo(app.Id);

        var dbRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == role.Id);
        await Assert.That(dbRole).IsNotNull();
    }

    [Test]
    public async Task CreateAsync_ShouldReturnNotFoundError_WhenApplicationDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var request = new CreateRoleRequestDto { Name = "Admin" };

        // Act
        var result = await service.CreateAsync(Guid.NewGuid(), request);

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Application not found.");
    }

    [Test]
    public async Task CreateAsync_ShouldReturnConflictError_WhenRoleNameAlreadyExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var existingRole = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        dbContext.Applications.Add(app);
        dbContext.Roles.Add(existingRole);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new CreateRoleRequestDto { Name = "Admin" };

        // Act
        var result = await service.CreateAsync(app.Id, request);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("already exists")).IsTrue();
    }

    [Test]
    public async Task ListAsync_ShouldReturnAllRoles_WhenApplicationExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var role1 = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var role2 = TestDataBuilder.CreateRole(app.Id, name: "User");
        dbContext.Applications.Add(app);
        dbContext.Roles.AddRange(role1, role2);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.ListAsync(app.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var roles = result.AsT0;
        await Assert.That(roles.Count).IsEqualTo(2);
        await Assert.That(roles[0].Name).IsEqualTo("Admin");
        await Assert.That(roles[1].Name).IsEqualTo("User");
    }

    [Test]
    public async Task ListAsync_ShouldReturnEmptyList_WhenNoRolesExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.ListAsync(app.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var roles = result.AsT0;
        await Assert.That(roles.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnRole_WhenExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        dbContext.Applications.Add(app);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetByIdAsync(app.Id, role.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();
        var roleDto = result.AsT0;
        await Assert.That(roleDto.Id).IsEqualTo(role.Id);
        await Assert.That(roleDto.Name).IsEqualTo("Admin");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNotFoundError_WhenRoleDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.GetByIdAsync(app.Id, Guid.NewGuid());

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Role not found.");
    }

    [Test]
    public async Task AssignPermissionAsync_ShouldAssignPermission_WhenRoleAndPermissionExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var user = TestDataBuilder.CreateUser(userName: "testuser");
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Test", action: "Read");
        var userRole = TestDataBuilder.CreateUserRole(user.Id, role.Id);
        dbContext.Applications.Add(app);
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        dbContext.UserRoles.Add(userRole);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.AssignPermissionAsync(app.Id, role.Id, permission.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var rolePermission = await dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
        await Assert.That(rolePermission).IsNotNull();

        permissionCache.Verify(x => x.InvalidateUserPermissionsAsync(user.Id, app.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AssignPermissionAsync_ShouldReturnConflictError_WhenPermissionAlreadyAssigned()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Test", action: "Read");
        var rolePermission = TestDataBuilder.CreateRolePermission(role.Id, permission.Id);
        dbContext.Applications.Add(app);
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        dbContext.RolePermissions.Add(rolePermission);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.AssignPermissionAsync(app.Id, role.Id, permission.Id);

        // Assert
        await Assert.That(result.IsT2).IsTrue();
        var error = result.AsT2;
        await Assert.That(error.Message.Contains("already has this permission")).IsTrue();
    }

    [Test]
    public async Task RemovePermissionAsync_ShouldRemovePermission_WhenRolePermissionExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        var permission = TestDataBuilder.CreatePermission(app.Id, module: "Test", action: "Read");
        var rolePermission = TestDataBuilder.CreateRolePermission(role.Id, permission.Id);
        dbContext.Applications.Add(app);
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        dbContext.RolePermissions.Add(rolePermission);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.RemovePermissionAsync(app.Id, role.Id, permission.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var removedRolePermission = await dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
        await Assert.That(removedRolePermission).IsNull();
    }

    [Test]
    public async Task DeleteAsync_ShouldDeleteRole_WhenExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        var role = TestDataBuilder.CreateRole(app.Id, name: "Admin");
        dbContext.Applications.Add(app);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var permissionCache = Helpers.MockFactory.CreatePermissionCache();
        var service = CreateService(dbContext, permissionCache);

        // Act
        var result = await service.DeleteAsync(app.Id, role.Id);

        // Assert
        await Assert.That(result.IsT0).IsTrue();

        var deletedRole = await dbContext.Roles.FindAsync([role.Id]);
        await Assert.That(deletedRole).IsNull();
    }

    [Test]
    public async Task DeleteAsync_ShouldReturnNotFoundError_WhenRoleDoesNotExist()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var app = TestDataBuilder.CreateApplication();
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // Act
        var result = await service.DeleteAsync(app.Id, Guid.NewGuid());

        // Assert
        await Assert.That(result.IsT1).IsTrue();
        var error = result.AsT1;
        await Assert.That(error.Message).IsEqualTo("Role not found.");
    }
}


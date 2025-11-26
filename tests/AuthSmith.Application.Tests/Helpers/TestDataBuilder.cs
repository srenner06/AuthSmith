using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Enums;
using App = AuthSmith.Domain.Entities.Application;

namespace AuthSmith.Application.Tests.Helpers;

/// <summary>
/// Builder for creating test data entities.
/// </summary>
public static class TestDataBuilder
{
    public static App CreateApplication(
        string? key = null,
        string? name = null,
        SelfRegistrationMode selfRegistrationMode = SelfRegistrationMode.Open,
        bool isActive = true)
    {
        return new App
        {
            Id = Guid.NewGuid(),
            Key = key ?? "testapp",
            Name = name ?? "Test Application",
            SelfRegistrationMode = selfRegistrationMode,
            IsActive = isActive,
            AccountLockoutEnabled = true,
            MaxFailedLoginAttempts = 5,
            LockoutDurationMinutes = 15,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static User CreateUser(
        string? userName = null,
        string? email = null,
        string? passwordHash = null,
        bool isActive = true)
    {
        var user = userName ?? "testuser";
        var userEmail = email ?? "test@example.com";
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = user,
            NormalizedUserName = user.ToUpperInvariant(),
            Email = userEmail,
            NormalizedEmail = userEmail.ToUpperInvariant(),
            PasswordHash = passwordHash ?? "hashed_password",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Role CreateRole(
        Guid applicationId,
        string? name = null,
        string? description = null)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Name = name ?? "TestRole",
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Permission CreatePermission(
        Guid applicationId,
        string? module = null,
        string? action = null,
        string? code = null,
        string? description = null)
    {
        var appKey = "testapp";
        var mod = module ?? "TestModule";
        var act = action ?? "Read";
        return new Permission
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Module = mod,
            Action = act,
            Code = code ?? $"{appKey}.{mod.ToLowerInvariant()}.{act.ToLowerInvariant()}",
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static UserRole CreateUserRole(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }

    public static RolePermission CreateRolePermission(Guid roleId, Guid permissionId)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };
    }

    public static UserPermission CreateUserPermission(Guid userId, Guid permissionId)
    {
        return new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId
        };
    }

    public static RefreshToken CreateRefreshToken(
        Guid userId,
        Guid applicationId,
        string? token = null,
        bool isRevoked = false)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = applicationId,
            Token = token ?? "test_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked,
            RevokedAt = isRevoked ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}


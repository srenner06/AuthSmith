using AuthSmith.Application.Services.Applications;
using AuthSmith.Application.Services.Auth;
using AuthSmith.Application.Services.Authorization;
using AuthSmith.Application.Services.Permissions;
using AuthSmith.Application.Services.Roles;
using AuthSmith.Application.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace AuthSmith.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountLockoutService, AccountLockoutService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}


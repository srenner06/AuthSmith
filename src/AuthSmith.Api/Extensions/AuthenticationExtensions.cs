using AuthSmith.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for configuring authentication and authorization.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        // API Key Authentication
        services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.DefaultScheme,
                options => { });

        // Authorization Policies
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAppAccess", policy =>
            {
                policy.RequireAssertion(context =>
                {
                    var accessLevel = context.User.FindFirst("AccessLevel")?.Value;
                    return accessLevel == "App" || accessLevel == "Admin";
                });
            })
            .AddPolicy("RequireAdminAccess", policy =>
            {
                policy.RequireAssertion(context =>
                {
                    var accessLevel = context.User.FindFirst("AccessLevel")?.Value;
                    return accessLevel == "Admin";
                });
            });

        return services;
    }
}

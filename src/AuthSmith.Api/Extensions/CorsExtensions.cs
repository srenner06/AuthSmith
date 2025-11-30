using AuthSmith.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for configuring CORS.
/// </summary>
public static class CorsExtensions
{
    public static IServiceCollection AddConfiguredCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsConfig = configuration
            .GetSection(CorsConfiguration.SectionName)
            .Get<CorsConfiguration>() ?? new();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowAll = corsConfig.AllowedOrigins.Contains("*");

                if (allowAll)
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.WithOrigins(corsConfig.AllowedOrigins);
                }

                policy.WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                      .WithHeaders("Content-Type", "Authorization")
                      .WithHeaders(corsConfig.AllowedHeaders)
                      .WithExposedHeaders(corsConfig.ExposedHeaders)
                      .SetPreflightMaxAge(TimeSpan.FromSeconds(corsConfig.MaxAge));

                if (corsConfig.AllowCredentials && !allowAll)
                {
                    policy.AllowCredentials();
                }
            });
        });

        return services;
    }
}

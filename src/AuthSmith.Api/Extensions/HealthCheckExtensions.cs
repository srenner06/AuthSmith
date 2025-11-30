using AuthSmith.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for configuring health checks.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly string[] ReadyTags = ["ready"];

    public static IServiceCollection AddConfiguredHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add database health check if not using in-memory
        var dbConfig = configuration
            .GetSection(DatabaseConfiguration.SectionName)
            .Get<DatabaseConfiguration>() ?? new();

        var dbConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? dbConfig.ConnectionString;

        if (!string.IsNullOrEmpty(dbConnectionString) &&
            !dbConnectionString.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            healthChecksBuilder.AddNpgSql(dbConnectionString, name: "database", tags: ReadyTags);
        }

        // Add JWT key health check
        var jwtConfig = configuration
            .GetSection(JwtConfiguration.SectionName)
            .Get<JwtConfiguration>() ?? new();

        healthChecksBuilder.AddCheck("jwt-key", () =>
        {
            if (string.IsNullOrWhiteSpace(jwtConfig.PrivateKeyPath) ||
                !File.Exists(jwtConfig.PrivateKeyPath))
            {
                return HealthCheckResult.Unhealthy("JWT private key not found");
            }
            return HealthCheckResult.Healthy();
        }, tags: ReadyTags);

        return services;
    }
}

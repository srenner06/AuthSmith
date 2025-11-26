using AuthSmith.Infrastructure.Configuration;
using AuthSmith.Infrastructure.Services.Authentication;
using AuthSmith.Infrastructure.Services.Caching;
using AuthSmith.Infrastructure.Services.Database;
using AuthSmith.Infrastructure.Services.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthSmith.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static partial class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate configuration classes
        services.Configure<DatabaseConfiguration>(configuration.GetSection(DatabaseConfiguration.SectionName));
        services.Configure<ApiKeysConfiguration>(configuration.GetSection(ApiKeysConfiguration.SectionName));
        services.Configure<JwtConfiguration>(configuration.GetSection(JwtConfiguration.SectionName));
        services.Configure<RedisConfiguration>(configuration.GetSection(RedisConfiguration.SectionName));
        services.Configure<OpenTelemetryConfiguration>(configuration.GetSection(OpenTelemetryConfiguration.SectionName));

        // Validate and get database configuration
        var databaseConfig = configuration.GetSection(DatabaseConfiguration.SectionName).Get<DatabaseConfiguration>()
            ?? new DatabaseConfiguration();
        
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? databaseConfig.ConnectionString
            ?? throw new InvalidOperationException("Database connection string not configured. Set 'ConnectionStrings:DefaultConnection' or 'Database:ConnectionString'.");

        // Only register database if not using in-memory (for tests)
        var isInMemory = connectionString.Equals("InMemory", StringComparison.OrdinalIgnoreCase) ||
                         databaseConfig.ConnectionString?.Equals("InMemory", StringComparison.OrdinalIgnoreCase) == true;
        
        if (!isInMemory)
        {
            services.AddDbContext<AuthSmithDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        // Password and API key hashing
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IApiKeyHasher, Argon2ApiKeyHasher>();

        // API key validation
        services.AddScoped<IApiKeyValidator, ApiKeyValidator>();

        // JWT token service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Refresh token service
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Permission caching
        var redisConfig = configuration.GetSection(RedisConfiguration.SectionName).Get<RedisConfiguration>()
            ?? new RedisConfiguration();
        
        if (redisConfig.Enabled && !string.IsNullOrWhiteSpace(redisConfig.ConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                try
                {
                    return ConnectionMultiplexer.Connect(redisConfig.ConnectionString);
                }
                catch (Exception ex)
                {
                    // Log and fallback to in-memory
                    var logger = sp.GetRequiredService<ILogger<RedisPermissionCache>>();
                    LogFailedToConnectToRedis(logger, ex);
                    throw; // Will be caught by the factory below
                }
            });

            // Try Redis first, fallback to in-memory if connection fails
            services.AddMemoryCache();
            services.AddScoped<IPermissionCache>(sp =>
            {
                try
                {
                    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                    return new RedisPermissionCache(redis, sp.GetRequiredService<ILogger<RedisPermissionCache>>());
                }
                catch
                {
                    return new InMemoryPermissionCache(
                        sp.GetRequiredService<IMemoryCache>(),
                        sp.GetRequiredService<ILogger<InMemoryPermissionCache>>());
                }
            });
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<IPermissionCache, InMemoryPermissionCache>();
        }

        // Database migrator
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        return services;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to connect to Redis, falling back to in-memory cache")]
    private static partial void LogFailedToConnectToRedis(ILogger logger, Exception ex);
}


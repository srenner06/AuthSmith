using AuthSmith.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthSmith.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Use a unique database name per factory instance
    // This ensures each test gets its own isolated database, but all scopes within the same factory share the same database
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    // Temporary RSA key files for JWT token generation in tests
    private readonly (string PrivateKeyPath, string PublicKeyPath) _rsaKeys;

    public CustomWebApplicationFactory()
    {
        // Create temporary RSA keys for this factory instance
        _rsaKeys = Helpers.TestHelpers.CreateTemporaryRsaKeyFiles();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variable to indicate test mode
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration first (before other sources)
            // This ensures it takes precedence
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "InMemory" },
                { "Database:ConnectionString", "InMemory" },
                { "Database:AutoMigrate", "false" },
                { "Jwt:PrivateKeyPath", _rsaKeys.PrivateKeyPath },
                { "Jwt:PublicKeyPath", _rsaKeys.PublicKeyPath },
                { "Jwt:Issuer", "https://test.identity.srenner.dev" },
                { "Jwt:Audience", "test-authsmith-api" },
                { "Jwt:ExpirationMinutes", "15" },
                { "Redis:Enabled", "false" },
                { "Logging:LogLevel:Default", "Warning" },
                { "AllowedHosts", "*" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL existing DbContext and DbContextOptions registrations first
            var toRemove = new List<ServiceDescriptor>();

            // Collect all services to remove
            foreach (var service in services.ToList())
            {
                // Remove DbContext registrations
                if (service.ServiceType == typeof(AuthSmithDbContext) ||
                    service.ServiceType == typeof(DbContextOptions<AuthSmithDbContext>))
                {
                    toRemove.Add(service);
                }

                // Remove any generic DbContextOptions
                if (service.ServiceType.IsGenericType &&
                    service.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                {
                    toRemove.Add(service);
                }

                // Remove health checks
                if (service.ServiceType == typeof(Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck))
                {
                    toRemove.Add(service);
                }

                // Remove any EF Core internal services that might be provider-specific
                // These are registered by UseNpgsql
                if (service.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true ||
                    service.ServiceType.Namespace?.StartsWith("Npgsql.EntityFrameworkCore", StringComparison.Ordinal) == true ||
                    service.ImplementationType?.Namespace?.StartsWith("Npgsql.EntityFrameworkCore", StringComparison.Ordinal) == true)
                {
                    // Only remove if it's not a core EF service that we need
                    if (!service.ServiceType.Name.Contains("ModelCache") &&
                        !service.ServiceType.Name.Contains("ValueGeneratorCache") &&
                        service.ServiceType.Name != "IDbContextOptionsExtension")
                    {
                        toRemove.Add(service);
                    }
                }
            }

            // Remove all collected services
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // Now add in-memory database with an instance-specific database name
            // This ensures all scopes (test and API) within this factory use the same database instance
            // Each factory instance gets its own isolated database for test isolation
            services.AddDbContext<AuthSmithDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: _databaseName)
                    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean up temporary RSA key files
            try
            {
                if (File.Exists(_rsaKeys.PrivateKeyPath))
                {
                    File.Delete(_rsaKeys.PrivateKeyPath);
                }
                if (File.Exists(_rsaKeys.PublicKeyPath))
                {
                    File.Delete(_rsaKeys.PublicKeyPath);
                }
                // Try to delete the directory if it's empty
                var keyDir = Path.GetDirectoryName(_rsaKeys.PrivateKeyPath);
                if (!string.IsNullOrEmpty(keyDir) && Directory.Exists(keyDir))
                {
                    try
                    {
                        Directory.Delete(keyDir);
                    }
                    catch
                    {
                        // Ignore if directory is not empty or can't be deleted
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        base.Dispose(disposing);
    }
}


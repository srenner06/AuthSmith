using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthSmith.Infrastructure.Services.Database;

/// <summary>
/// Service for running database migrations on startup.
/// </summary>
public interface IDatabaseMigrator
{
    /// <summary>
    /// Applies pending migrations to the database.
    /// </summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Database migration runner that applies migrations on startup.
/// </summary>
public class DatabaseMigrator : IDatabaseMigrator
{
    private readonly AuthSmith.Infrastructure.AuthSmithDbContext _dbContext;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(AuthSmith.Infrastructure.AuthSmithDbContext dbContext, ILogger<DatabaseMigrator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Skip migration for in-memory databases (used in tests)
            // Check the provider name without triggering full initialization
            var providerName = _dbContext.Database.ProviderName;
            if (providerName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                _logger.LogInformation("Skipping migration for in-memory database");
                return;
            }

            _logger.LogInformation("Applying database migrations...");
            await _dbContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migrations applied successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("database providers") && ex.Message.Contains("InMemory"))
        {
            // If we get the provider conflict error and InMemory is involved, skip migration
            _logger.LogInformation("Skipping migration for in-memory database");
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }
}


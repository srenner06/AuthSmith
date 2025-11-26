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
public partial class DatabaseMigrator : IDatabaseMigrator
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
                LogSkippingInMemoryMigration(_logger);
                return;
            }

            LogApplyingMigrations(_logger);
            await _dbContext.Database.MigrateAsync(cancellationToken);
            LogMigrationsApplied(_logger);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("database providers") && ex.Message.Contains("InMemory"))
        {
            // If we get the provider conflict error and InMemory is involved, skip migration
            LogSkippingInMemoryMigration(_logger);
            return;
        }
        catch (Exception ex)
        {
            LogFailedToApplyMigrations(_logger, ex);
            throw;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Applying database migrations...")]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Database migrations applied successfully")]
    private static partial void LogMigrationsApplied(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to apply database migrations")]
    private static partial void LogFailedToApplyMigrations(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Skipping migration for in-memory database")]
    private static partial void LogSkippingInMemoryMigration(ILogger logger);
}


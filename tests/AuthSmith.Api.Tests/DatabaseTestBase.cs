using AuthSmith.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthSmith.Api.Tests;

/// <summary>
/// Base class for integration tests with in-memory database.
/// </summary>
public abstract class DatabaseTestBase : IDisposable
{
    protected AuthSmithDbContext DbContext { get; }
    protected IServiceProvider ServiceProvider { get; }

    protected DatabaseTestBase()
    {
        var serviceCollection = new ServiceCollection();
        
        // Add in-memory database
        serviceCollection.AddDbContext<AuthSmithDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Add logging
        serviceCollection.AddLogging(builder => builder.AddConsole());

        // Add infrastructure services needed for tests
        serviceCollection.AddMemoryCache();
        
        ServiceProvider = serviceCollection.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<AuthSmithDbContext>();
        
        // Ensure database is created
        DbContext.Database.EnsureCreated();
    }

    protected async Task SeedDatabaseAsync(Action<AuthSmithDbContext> seedAction)
    {
        seedAction(DbContext);
        await DbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        (ServiceProvider as IDisposable)?.Dispose();
    }
}


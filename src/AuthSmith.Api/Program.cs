using AuthSmith.Api.Extensions;
using AuthSmith.Application;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Configuration;
using AuthSmith.Infrastructure.Services.Database;
using Serilog;

namespace AuthSmith.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Initial bootstrap logger (minimal)
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting AuthSmith API");

            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog with all sinks (console, file, OTLP)
            Log.Logger = new LoggerConfiguration()
                .ConfigureAuthSmithLogger(builder.Configuration, builder.Environment.EnvironmentName)
                .CreateLogger();

            // Use Serilog for all logging
            builder.Host.UseSerilog();

            // Debug: Log configuration
            LogConfigurationDebugInfo(builder.Configuration);

            // Configure services
            builder.Services
                .AddHttpContextAccessor() // Required for RequestContextService
                .AddOpenTelemetryObservability(builder.Configuration, builder.Environment.EnvironmentName)
                .AddConfiguredCors(builder.Configuration)
                .Configure<RateLimitConfiguration>(builder.Configuration.GetSection(RateLimitConfiguration.SectionName))
                .AddApiServices()
                .AddInfrastructure(builder.Configuration)
                .AddApplication()
                .AddApiAuthentication()
                .AddConfiguredHealthChecks(builder.Configuration);

            // Register Request Context Service
            builder.Services.AddScoped<AuthSmith.Application.Services.Context.IRequestContextService, AuthSmith.Api.Services.RequestContextService>();

            var app = builder.Build();

            // Apply database migrations if configured
            await ApplyDatabaseMigrationsAsync(app.Services, builder.Configuration);

            // Configure middleware pipeline
            app.ConfigureMiddleware();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void LogConfigurationDebugInfo(ConfigurationManager configuration)
    {
        Log.Information("Configuration sources:");
        foreach (var source in configuration.Sources)
        {
            Log.Information("  - {SourceType}", source.GetType().Name);
        }

        Log.Information("RateLimit configuration from IConfiguration:");
        Log.Information("  RateLimit:Enabled = {Value}", configuration["RateLimit:Enabled"]);
        Log.Information("  RateLimit:GeneralLimit = {Value}", configuration["RateLimit:GeneralLimit"]);
        Log.Information("  RateLimit:AuthLimit = {Value}", configuration["RateLimit:AuthLimit"]);
        Log.Information("  RateLimit:RegistrationLimit = {Value}", configuration["RateLimit:RegistrationLimit"]);
        Log.Information("  RateLimit:PasswordResetLimit = {Value}", configuration["RateLimit:PasswordResetLimit"]);
        Log.Information("  RateLimit:WindowSeconds = {Value}", configuration["RateLimit:WindowSeconds"]);
    }

    private static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services, ConfigurationManager configuration)
    {
        var dbConfig = configuration
            .GetSection(DatabaseConfiguration.SectionName)
            .Get<DatabaseConfiguration>() ?? new();

        if (dbConfig.AutoMigrate)
        {
            using var scope = services.CreateScope();
            var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
            await migrator.MigrateAsync();
        }
    }
}

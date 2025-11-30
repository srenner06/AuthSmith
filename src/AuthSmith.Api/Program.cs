using System.Text.Json;
using Asp.Versioning;
using AuthSmith.Api.Authentication;
using AuthSmith.Api.Extensions;
using AuthSmith.Api.Filters;
using AuthSmith.Api.Middleware;
using AuthSmith.Application;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Configuration;
using AuthSmith.Infrastructure.Services.Database;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NSwag;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace AuthSmith.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog before building the host
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/authsmith-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting AuthSmith API");

            var builder = WebApplication.CreateBuilder(args);

            // Use Serilog for logging
            builder.Host.UseSerilog();

            // Configure services
            builder.Services
                .AddOpenTelemetryObservability(builder.Configuration, builder.Environment.EnvironmentName)
                .AddConfiguredCors(builder.Configuration)
                .Configure<RateLimitConfiguration>(builder.Configuration.GetSection(RateLimitConfiguration.SectionName))
                .AddApiServices()
                .AddInfrastructure(builder.Configuration)
                .AddApplication()
                .AddApiAuthentication()
                .AddConfiguredHealthChecks(builder.Configuration);

            var app = builder.Build();

            // Apply database migrations if configured
            var dbConfig = builder.Configuration
                .GetSection(DatabaseConfiguration.SectionName)
                .Get<DatabaseConfiguration>() ?? new();

            if (dbConfig.AutoMigrate)
            {
                using var scope = app.Services.CreateScope();
                var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
                await migrator.MigrateAsync();
            }

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
}

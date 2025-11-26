using System.Text.Json;
using Asp.Versioning;
using AuthSmith.Api.Authentication;
using AuthSmith.Api.Filters;
using AuthSmith.Api.Middleware;
using AuthSmith.Application;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Database;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NSwag;

namespace AuthSmith.Api;

public class Program
{
    private static readonly string[] ReadyTags = ["ready"];

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<FluentValidationFilter>();
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddValidatorsFromAssemblyContaining<AuthSmith.Application.Validators.RegisterRequestDtoValidator>();

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("version"),
                new HeaderApiVersionReader("X-Version")
            );
        });

        // Configure NSwag for OpenAPI generation
        builder.Services.AddOpenApiDocument(settings =>
        {
            settings.DocumentName = "v1";
            settings.Title = "AuthSmith API";
            settings.Version = "v1";
            settings.Description = "Identity and Authorization Service API - A comprehensive authentication and authorization service with support for API key and JWT token authentication.";

            // Add API Key security scheme
            settings.AddSecurity("ApiKey", new NSwag.OpenApiSecurityScheme
            {
                Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                Name = "X-API-Key",
                Description = "API Key authentication using X-API-Key header."
            });

            // Additional NSwag enhancements
            settings.UseControllerSummaryAsTagDescription = true;

            // Apply security requirements conditionally based on [AllowAnonymous] attribute
            settings.OperationProcessors.Add(new NswagSecurityOperationProcessor());
        });

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();

        builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.DefaultScheme,
                options => { });

        builder.Services.AddAuthorizationBuilder()
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

        var healthChecksBuilder = builder.Services.AddHealthChecks();
        
        // Get configuration via IOptions for health checks
        var dbConfig = builder.Configuration.GetSection(AuthSmith.Infrastructure.Configuration.DatabaseConfiguration.SectionName)
            .Get<AuthSmith.Infrastructure.Configuration.DatabaseConfiguration>() ?? new();
        
        var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? dbConfig.ConnectionString;
        
        // Only add database health check if not using in-memory database (for tests)
        if (!string.IsNullOrEmpty(dbConnectionString) && !dbConnectionString.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            healthChecksBuilder.AddNpgSql(dbConnectionString, name: "database", tags: ReadyTags);
        }
        
        var jwtConfig = builder.Configuration.GetSection(AuthSmith.Infrastructure.Configuration.JwtConfiguration.SectionName)
            .Get<AuthSmith.Infrastructure.Configuration.JwtConfiguration>() ?? new();
        
        healthChecksBuilder.AddCheck("jwt-key", () =>
        {
            if (string.IsNullOrWhiteSpace(jwtConfig.PrivateKeyPath) || !File.Exists(jwtConfig.PrivateKeyPath))
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("JWT private key not found");
            }
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
        }, tags: ReadyTags);

        var app = builder.Build();

        if (dbConfig.AutoMigrate)
        {
            using var scope = app.Services.CreateScope();
            var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
            await migrator.MigrateAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            // Use NSwag OpenAPI document and Swagger UI
            app.UseOpenApi();
            app.UseSwaggerUi(settings =>
            {
                settings.DocumentPath = "/swagger/v1/swagger.json";
                settings.Path = "/swagger";
                settings.EnableTryItOut = true;
                settings.PersistAuthorization = true; // Persist auth across page refreshes
                settings.DocExpansion = "list";
                settings.DefaultModelsExpandDepth = 2;
                settings.DefaultModelExpandDepth = 2;
            });
        }

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ProblemDetailsMiddleware>();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        exception = e.Value.Exception?.Message,
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        await app.RunAsync();
    }
}

using System.Text.Json;
using AuthSmith.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for configuring middleware pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        // Security headers - apply before other middleware
        app.UseSecurityHeaders();

        // Rate limiting - apply early to protect endpoints
        app.UseRateLimiting();

        // Swagger (development only)
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi(settings =>
            {
                settings.DocumentPath = "/swagger/v1/swagger.json";
                settings.Path = "/swagger";
                settings.EnableTryItOut = true;
                settings.PersistAuthorization = true;
                settings.DocExpansion = "list";
                settings.DefaultModelsExpandDepth = 2;
                settings.DefaultModelExpandDepth = 2;
            });
        }

        // Request logging and error handling
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ProblemDetailsMiddleware>();

        // Standard ASP.NET Core middleware
        app.UseHttpsRedirection();
        app.UseCors(); // CORS must be before Authentication/Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Controllers
        app.MapControllers();

        // Health checks
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

        return app;
    }
}

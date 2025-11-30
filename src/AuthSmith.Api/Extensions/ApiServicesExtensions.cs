using System.Text.Json.Serialization;
using Asp.Versioning;
using AuthSmith.Api.Filters;
using FluentValidation;
using NSwag;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for configuring API services (Swagger, Versioning, Validation).
/// </summary>
public static class ApiServicesExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Controllers with validation filter and JSON options
        services.AddControllers(options =>
        {
            options.Filters.Add<FluentValidationFilter>();
        })
        .AddJsonOptions(options =>
        {
            // Serialize enums as strings instead of integers
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddEndpointsApiExplorer();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<AuthSmith.Application.Validators.RegisterRequestDtoValidator>();

        // API Versioning
        services.AddApiVersioning(options =>
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

        // Swagger/OpenAPI with NSwag
        services.AddOpenApiDocument(settings =>
        {
            settings.DocumentName = "v1";
            settings.Title = "AuthSmith API";
            settings.Version = "v1";
            settings.Description = "Identity and Authorization Service API - A comprehensive authentication and authorization service with support for API key and JWT token authentication.";

            // Add API Key security scheme
            settings.AddSecurity("ApiKey", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                In = OpenApiSecurityApiKeyLocation.Header,
                Name = "X-API-Key",
                Description = "API Key authentication using X-API-Key header."
            });

            settings.UseControllerSummaryAsTagDescription = true;
            settings.OperationProcessors.Add(new NswagSecurityOperationProcessor());
        });

        return services;
    }
}

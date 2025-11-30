using AuthSmith.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        var otelConfig = configuration
            .GetSection(OpenTelemetryConfiguration.SectionName)
            .Get<OpenTelemetryConfiguration>() ?? new();

        if (!otelConfig.Enabled)
        {
            Log.Information("OpenTelemetry is disabled");
            return services;
        }

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: otelConfig.ServiceName,
                serviceVersion: otelConfig.ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environmentName,
                ["host.name"] = Environment.MachineName
            });

        // Configure Tracing
        if (otelConfig.EnableTracing)
        {
            services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    ConfigureTracing(tracerProviderBuilder, resourceBuilder, otelConfig);
                });
        }

        // Configure Metrics
        if (otelConfig.EnableMetrics)
        {
            services.AddOpenTelemetry()
                .WithMetrics(meterProviderBuilder =>
                {
                    ConfigureMetrics(meterProviderBuilder, resourceBuilder, otelConfig);
                });
        }

        Log.Information("OpenTelemetry configured - Service: {ServiceName}, Endpoint: {Endpoint}",
            otelConfig.ServiceName, otelConfig.Endpoint);

        return services;
    }

    private static void ConfigureTracing(
        TracerProviderBuilder tracerProviderBuilder,
        ResourceBuilder resourceBuilder,
        OpenTelemetryConfiguration config)
    {
        tracerProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress);
                };
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.RecordException = true;
            });

        // Add console exporter for development
        if (config.UseConsoleExporter)
        {
            tracerProviderBuilder.AddConsoleExporter();
        }

        // Add OTLP exporter if endpoint is configured
        if (!string.IsNullOrWhiteSpace(config.Endpoint))
        {
            tracerProviderBuilder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(config.Endpoint);

                // Add custom headers if configured
                if (config.Headers.Any())
                {
                    foreach (var header in config.Headers)
                    {
                        options.Headers = $"{options.Headers},{header.Key}={header.Value}";
                    }
                }
            });
        }
    }

    private static void ConfigureMetrics(
        MeterProviderBuilder meterProviderBuilder,
        ResourceBuilder resourceBuilder,
        OpenTelemetryConfiguration config)
    {
        meterProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        // Add console exporter for development
        if (config.UseConsoleExporter)
        {
            meterProviderBuilder.AddConsoleExporter();
        }

        // Add OTLP exporter if endpoint is configured
        if (!string.IsNullOrWhiteSpace(config.Endpoint))
        {
            meterProviderBuilder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(config.Endpoint);

                // Add custom headers if configured
                if (config.Headers.Any())
                {
                    foreach (var header in config.Headers)
                    {
                        options.Headers = $"{options.Headers},{header.Key}={header.Value}";
                    }
                }
            });
        }
    }
}

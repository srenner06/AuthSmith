using AuthSmith.Infrastructure.Configuration;
using Serilog;
using Serilog.Events;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for configuring Serilog.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog with console, file, and OTLP sinks.
    /// </summary>
    public static LoggerConfiguration ConfigureAuthSmithLogger(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        string environmentName)
    {
        // Get OpenTelemetry configuration
        var otelConfig = configuration
            .GetSection(OpenTelemetryConfiguration.SectionName)
            .Get<OpenTelemetryConfiguration>() ?? new();

        // Get Serilog minimum levels from configuration
        var defaultLevel = configuration["Serilog:MinimumLevel:Default"];
        var microsoftLevel = configuration["Serilog:MinimumLevel:Override:Microsoft"];
        var systemLevel = configuration["Serilog:MinimumLevel:Override:System"];

        // Parse log levels with fallbacks
        var minimumLevel = Enum.TryParse<LogEventLevel>(defaultLevel, out var level)
            ? level
            : LogEventLevel.Information;

        var microsoftMinLevel = Enum.TryParse<LogEventLevel>(microsoftLevel, out var msLevel)
            ? msLevel
            : LogEventLevel.Warning;

        var systemMinLevel = Enum.TryParse<LogEventLevel>(systemLevel, out var sysLevel)
            ? sysLevel
            : LogEventLevel.Warning;

        // Configure Serilog
        loggerConfiguration
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", microsoftMinLevel)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", systemMinLevel)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("service.name", otelConfig.ServiceName)
            .Enrich.WithProperty("service.version", otelConfig.ServiceVersion)
            .Enrich.WithProperty("deployment.environment", environmentName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/authsmith-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");

        // Add OTLP sink if enabled
        if (otelConfig.Enabled && otelConfig.EnableLogs && !string.IsNullOrWhiteSpace(otelConfig.Endpoint))
        {
            ConfigureOtlpSink(loggerConfiguration, otelConfig);
        }
        else
        {
            Log.Warning("OTLP logging sink not configured - Enabled: {Enabled}, EnableLogs: {EnableLogs}, Endpoint: {Endpoint}",
                otelConfig.Enabled, otelConfig.EnableLogs, otelConfig.Endpoint);
        }

        return loggerConfiguration;
    }

    /// <summary>
    /// Configures Serilog to send logs to OTLP endpoint (Aspire Dashboard).
    /// </summary>
    private static void ConfigureOtlpSink(
        LoggerConfiguration loggerConfiguration,
        OpenTelemetryConfiguration otelConfig)
    {
        try
        {
            // Convert gRPC endpoint (18889) to HTTP endpoint (18890) for Serilog OTLP sink
            var otlpEndpoint = otelConfig.Endpoint.Replace(":18889", ":18890");

            loggerConfiguration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;

                // Add resource attributes
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = otelConfig.ServiceName,
                    ["service.version"] = otelConfig.ServiceVersion
                };

                // Add custom headers if configured
                if (otelConfig.Headers.Count != 0)
                {
                    options.Headers = otelConfig.Headers;
                }
            });

            Log.Information("Serilog OTLP sink configured - Endpoint: {Endpoint}", otlpEndpoint);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to configure Serilog OTLP sink");
        }
    }
}

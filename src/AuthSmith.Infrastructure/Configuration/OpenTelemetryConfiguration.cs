namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// Configuration for OpenTelemetry distributed tracing and metrics.
/// </summary>
public class OpenTelemetryConfiguration
{
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// Whether OpenTelemetry is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// OTLP endpoint URL (e.g., "http://aspire-dashboard:18889", "http://localhost:18889", or "http://jaeger:4317").
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Service name for telemetry (defaults to "AuthSmith").
    /// </summary>
    public string ServiceName { get; set; } = "AuthSmith";

    /// <summary>
    /// Service version (defaults to build version from VersionInfo).
    /// </summary>
    public string ServiceVersion { get; set; } = VersionInfo.GetVersion();

    /// <summary>
    /// Whether to use console exporter for development.
    /// </summary>
    public bool UseConsoleExporter { get; set; }

    /// <summary>
    /// Whether to enable tracing.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Whether to enable metrics.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Whether to enable logs export via OTLP.
    /// </summary>
    public bool EnableLogs { get; set; } = true;

    /// <summary>
    /// Additional headers for OTLP export (e.g., authentication).
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = [];
}


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
    /// OTLP endpoint URL (e.g., "http://jaeger:4317" or "http://localhost:4317").
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Service name for telemetry (defaults to "AuthSmith").
    /// </summary>
    public string ServiceName { get; set; } = "AuthSmith";

    /// <summary>
    /// Service version (can be set from build).
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

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
    /// Additional headers for OTLP export (e.g., authentication).
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}


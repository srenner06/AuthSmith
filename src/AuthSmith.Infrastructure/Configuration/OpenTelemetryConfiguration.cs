namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// OpenTelemetry configuration settings.
/// </summary>
public class OpenTelemetryConfiguration
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
}


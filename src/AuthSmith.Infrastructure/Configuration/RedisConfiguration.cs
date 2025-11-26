namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// Redis configuration settings.
/// </summary>
public class RedisConfiguration
{
    public const string SectionName = "Redis";

    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
}


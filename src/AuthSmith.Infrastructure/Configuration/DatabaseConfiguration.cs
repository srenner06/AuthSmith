namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// Database configuration settings.
/// </summary>
public class DatabaseConfiguration
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
    public bool AutoMigrate { get; set; } = true;
}


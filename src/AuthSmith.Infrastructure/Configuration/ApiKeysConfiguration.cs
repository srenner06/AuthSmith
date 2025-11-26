namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// API keys configuration settings.
/// </summary>
public class ApiKeysConfiguration
{
    public const string SectionName = "ApiKeys";

    public List<string> Admin { get; set; } = [];
    public string Bootstrap { get; set; } = string.Empty;
}


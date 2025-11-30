namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// API keys configuration settings.
/// </summary>
public class ApiKeysConfiguration
{
    public const string SectionName = "ApiKeys";

    /// <summary>
    /// List of admin API keys that have full administrative access.
    /// These keys can manage applications, users, roles, and permissions.
    /// </summary>
    public List<string> Admin { get; set; } = [];
}


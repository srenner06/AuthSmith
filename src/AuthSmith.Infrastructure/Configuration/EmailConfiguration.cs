namespace AuthSmith.Infrastructure.Configuration;

/// <summary>
/// Email service configuration for SMTP.
/// </summary>
public class EmailConfiguration
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Enable SSL/TLS for SMTP connection.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// SMTP username for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SMTP password for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// From email address.
    /// </summary>
    public string FromAddress { get; set; } = "noreply@authsmith.local";

    /// <summary>
    /// From display name.
    /// </summary>
    public string FromName { get; set; } = "AuthSmith";

    /// <summary>
    /// Enable email sending (disable for testing).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Base URL for email links (password reset, verification).
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:5001";
}

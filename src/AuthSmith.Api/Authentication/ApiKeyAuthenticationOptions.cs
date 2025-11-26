using Microsoft.AspNetCore.Authentication;

namespace AuthSmith.Api.Authentication;

/// <summary>
/// Options for API key authentication.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string HeaderName { get; set; } = "X-API-Key";
    public string BearerScheme { get; set; } = "Bearer";
}


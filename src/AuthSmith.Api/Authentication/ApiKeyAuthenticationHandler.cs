using System.Security.Claims;
using System.Text.Encodings.Web;
using AuthSmith.Infrastructure.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthSmith.Api.Authentication;

/// <summary>
/// Authentication handler for API key validation.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyValidator _apiKeyValidator;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator apiKeyValidator)
        : base(options, logger, encoder)
    {
        _apiKeyValidator = apiKeyValidator;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? apiKey = null;

        // Try custom header first - use direct indexer access (IHeaderDictionary is case-insensitive)
        var customHeader = Request.Headers[Options.HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(customHeader))
        {
            apiKey = customHeader;
        }
        // Also try case-insensitive lookup by iterating headers as fallback
        else
        {
            foreach (var header in Request.Headers)
            {
                if (string.Equals(header.Key, Options.HeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    apiKey = header.Value.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        break;
                    }
                }
            }
        }

        // Try Authorization header with Bearer scheme if custom header not found
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader) &&
                authHeader.StartsWith(Options.BearerScheme + " ", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = authHeader.Substring(Options.BearerScheme.Length + 1).Trim();
            }
        }

        // Debug logging if no API key found
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                var availableHeaders = string.Join(", ", Request.Headers.Select(h => h.Key));
                Logger.LogDebug("No API key found in request headers. Expected header: {Header}. Available headers: {Headers}", Options.HeaderName, availableHeaders);
            }
            return AuthenticateResult.NoResult();
        }

        var validationResult = await _apiKeyValidator.ValidateAsync(apiKey);

        if (!validationResult.IsValid)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "ApiKey"),
            new Claim("AccessLevel", validationResult.AccessLevel.ToString())
        };

        if (validationResult.ApplicationId.HasValue)
        {
            claims.Add(new Claim("ApplicationId", validationResult.ApplicationId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}


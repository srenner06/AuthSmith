using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Errors;
using AuthSmith.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OneOf;

namespace AuthSmith.Infrastructure.Services.Authentication;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    Task<OneOf<string, NotFoundError, FileNotFoundError>> GenerateAccessTokenAsync(
        User user,
        Application application,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// JWT token generation service using asymmetric keys (RSA/ECDSA).
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IOptions<JwtConfiguration> _jwtConfig;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IOptions<JwtConfiguration> jwtConfig, ILogger<JwtTokenService> logger)
    {
        _jwtConfig = jwtConfig;
        _logger = logger;
    }

    public async Task<OneOf<string, NotFoundError, FileNotFoundError>> GenerateAccessTokenAsync(
        User user,
        Application application,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName),
            new Claim("preferred_username", user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("app", application.Key)
        };

        // Add roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("roles", role));
        }

        // Add permissions
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        var keyResult = await GetSigningKeyAsync(cancellationToken);
        return keyResult.Match<OneOf<string, NotFoundError, FileNotFoundError>>(
            key =>
            {
                var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
                var config = _jwtConfig.Value;
                var token = new JwtSecurityToken(
                    issuer: config.Issuer,
                    audience: config.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(config.ExpirationMinutes),
                    signingCredentials: credentials
                );
                return new JwtSecurityTokenHandler().WriteToken(token);
            },
            notFoundError => notFoundError,
            fileNotFoundError => fileNotFoundError
        );

    }

    private async Task<OneOf<SecurityKey, NotFoundError, FileNotFoundError>> GetSigningKeyAsync(CancellationToken cancellationToken)
    {
        var config = _jwtConfig.Value;
        if (string.IsNullOrWhiteSpace(config.PrivateKeyPath))
        {
            return NotFoundError.Instance;
        }

        if (!File.Exists(config.PrivateKeyPath))
        {
            return new FileNotFoundError(config.PrivateKeyPath, $"JWT private key file not found: {config.PrivateKeyPath}");
        }

        var privateKeyPem = await File.ReadAllTextAsync(config.PrivateKeyPath, cancellationToken);
        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        return new RsaSecurityKey(rsa);
    }
}



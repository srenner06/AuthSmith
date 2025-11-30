using System.Security.Cryptography;
using AuthSmith.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route(".well-known")]
[AllowAnonymous]
public class JwksController : ControllerBase
{
    private readonly IOptions<JwtConfiguration> _jwtConfig;
    private readonly ILogger<JwksController> _logger;

    public JwksController(IOptions<JwtConfiguration> jwtConfig, ILogger<JwksController> logger)
    {
        _jwtConfig = jwtConfig;
        _logger = logger;
    }

    /// <summary>
    /// Get the public key in JWK format for JWT token validation.
    /// </summary>
    [HttpGet("jwks.json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJwksAsync(CancellationToken cancellationToken)
    {
        var publicKeyPath = _jwtConfig.Value.PublicKeyPath;
        if (string.IsNullOrWhiteSpace(publicKeyPath) || !System.IO.File.Exists(publicKeyPath))
        {
            return NotFound(new { error = "Public key not found" });
        }

        try
        {
            var publicKeyPem = await System.IO.File.ReadAllTextAsync(publicKeyPath, cancellationToken);
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var parameters = rsa.ExportParameters(false);

            var jwk = new
            {
                keys = new[]
                {
                    new
                    {
                        kty = "RSA",
                        use = "sig",
                        kid = "authsmith-key-1",
                        alg = "RS256",
                        n = Base64UrlEncode(parameters.Modulus!),
                        e = Base64UrlEncode(parameters.Exponent!)
                    }
                }
            };

            return Ok(jwk);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWK");
            return StatusCode(500, new { error = "Failed to generate JWK" });
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}


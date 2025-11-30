using System.Security.Cryptography;
using System.Text;
using AuthSmith.Application.Tests.Helpers;
using AuthSmith.Infrastructure;
using AuthSmith.Infrastructure.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using App = AuthSmith.Domain.Entities.Application;

namespace AuthSmith.Api.Tests.Helpers;

public static class TestHelpers
{
    /// <summary>
    /// Creates an application with an API key for testing. The application key is auto-generated to be unique.
    /// </summary>
    public static async Task<App> CreateApplicationWithApiKeyAsync(
        AuthSmithDbContext dbContext,
        IApiKeyHasher apiKeyHasher,
        string? apiKey = null)
    {
        var app = TestDataBuilder.CreateApplication();  // Auto-generates unique key
        if (apiKey != null)
        {
            app.ApiKeyHash = apiKeyHasher.HashApiKey(apiKey);
        }
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();
        return app;
    }

    /// <summary>
    /// Creates temporary RSA key files for JWT token generation in tests.
    /// Returns a tuple with (privateKeyPath, publicKeyPath).
    /// </summary>
    public static (string PrivateKeyPath, string PublicKeyPath) CreateTemporaryRsaKeyFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "AuthSmithTests", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        var privateKeyPath = Path.Combine(tempDir, "private_key.pem");
        var publicKeyPath = Path.Combine(tempDir, "public_key.pem");

        using var rsa = RSA.Create(2048);

        // Export private key in PEM format (PKCS#1 format)
        var privateKeyBytes = rsa.ExportRSAPrivateKey();
        var privateKeyPem = new StringBuilder();
        privateKeyPem.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
        privateKeyPem.AppendLine(Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks));
        privateKeyPem.AppendLine("-----END RSA PRIVATE KEY-----");
        File.WriteAllText(privateKeyPath, privateKeyPem.ToString());

        // Export public key in PEM format (PKCS#8 format - SubjectPublicKeyInfo)
        // ImportFromPem expects PKCS#8 format for public keys
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        var publicKeyPem = new StringBuilder();
        publicKeyPem.AppendLine("-----BEGIN PUBLIC KEY-----");
        publicKeyPem.AppendLine(Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks));
        publicKeyPem.AppendLine("-----END PUBLIC KEY-----");
        File.WriteAllText(publicKeyPath, publicKeyPem.ToString());

        return (privateKeyPath, publicKeyPath);
    }
}


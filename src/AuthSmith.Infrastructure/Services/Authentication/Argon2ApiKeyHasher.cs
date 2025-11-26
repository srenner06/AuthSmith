namespace AuthSmith.Infrastructure.Services.Authentication;

/// <summary>
/// API key hasher implementation that reuses the password hasher.
/// </summary>
public class Argon2ApiKeyHasher : IApiKeyHasher
{
    private readonly IPasswordHasher _passwordHasher;

    public Argon2ApiKeyHasher(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public string HashApiKey(string apiKey)
    {
        return _passwordHasher.HashPassword(apiKey);
    }

    public bool VerifyApiKey(string apiKey, string hash)
    {
        return _passwordHasher.VerifyPassword(apiKey, hash);
    }
}


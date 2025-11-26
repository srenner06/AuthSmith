namespace AuthSmith.Infrastructure.Services.Authentication;

/// <summary>
/// Service for hashing and verifying API keys.
/// Reuses the password hasher implementation.
/// </summary>
public interface IApiKeyHasher
{
    /// <summary>
    /// Hashes an API key.
    /// </summary>
    /// <param name="apiKey">The plain text API key to hash.</param>
    /// <returns>A PHC-formatted string containing the hash.</returns>
    string HashApiKey(string apiKey);

    /// <summary>
    /// Verifies an API key against a hash.
    /// </summary>
    /// <param name="apiKey">The plain text API key to verify.</param>
    /// <param name="hash">The PHC-formatted hash string.</param>
    /// <returns>True if the API key matches, false otherwise.</returns>
    bool VerifyApiKey(string apiKey, string hash);
}

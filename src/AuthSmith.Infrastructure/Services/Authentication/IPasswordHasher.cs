namespace AuthSmith.Infrastructure.Services.Authentication;

/// <summary>
/// Service for hashing and verifying passwords using Argon2id.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using Argon2id.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>A PHC-formatted string containing the hash, salt, and parameters.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hash">The PHC-formatted hash string.</param>
    /// <returns>True if the password matches, false otherwise.</returns>
    bool VerifyPassword(string password, string hash);
}

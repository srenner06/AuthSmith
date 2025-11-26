using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace AuthSmith.Infrastructure.Services.Authentication;

/// <summary>
/// Argon2id password hasher implementation following OWASP recommendations.
/// Uses PHC string format for storage.
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 16 bytes = 128 bits
    private const int MemorySize = 65536; // 64 MB
    private const int Iterations = 3;
    private const int DegreeOfParallelism = 4;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        // Generate random salt
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash the password
        var hash = HashPasswordInternal(password, salt);

        // Format as PHC string: $argon2id$v=19$m=65536,t=3,p=4$salt$hash
        var saltBase64 = Convert.ToBase64String(salt);
        var hashBase64 = Convert.ToBase64String(hash);

        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}${saltBase64}${hashBase64}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            // Parse PHC string
            var parts = hash.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id")
                return false;

            // Extract parameters
            var paramsPart = parts[3];
            var saltBase64 = parts[4];
            var hashBase64 = parts[5];

            var salt = Convert.FromBase64String(saltBase64);

            // Hash the provided password with the same salt
            var computedHash = HashPasswordInternal(password, salt);
            var storedHash = Convert.FromBase64String(hashBase64);

            // Constant-time comparison
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] HashPasswordInternal(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        using var argon2 = new Argon2id(passwordBytes)
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            Iterations = Iterations,
            MemorySize = MemorySize
        };

        return argon2.GetBytes(32); // 32 bytes = 256 bits
    }
}


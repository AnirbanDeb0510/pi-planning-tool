using System.Security.Cryptography;

namespace PiPlanningBackend.Services.Utilities
{
    /// <summary>
    /// Utility class for password hashing and verification using PBKDF2.
    /// Implements industry-standard password hashing with salt and configurable iterations.
    /// </summary>
    public static class PasswordHelper
    {
        private const int SaltSize = 16; // 128 bits
        private const int Iterations = 10000; // NIST recommended minimum for password hashing
        private const int HashSize = 20; // 160 bits

        /// <summary>
        /// Hashes a password using PBKDF2 with a cryptographically random salt.
        /// Each call generates a unique hash even for the same password.
        /// </summary>
        /// <param name="password">The plain-text password to hash</param>
        /// <returns>Base64-encoded string in format "salt:hash" separated by colon</returns>
        /// <remarks>
        /// The returned string contains both salt and hash. The salt is stored with the hash
        /// to enable verification later. Do not attempt to decrypt or reverse this value.
        /// </remarks>
        public static string HashPassword(string password)
        {
            // Generate cryptographically random salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash password with salt using PBKDF2
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(HashSize);

            // Return salt:hash format (salt needed for verification)
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies a plain-text password against a stored PBKDF2 hash.
        /// </summary>
        /// <param name="password">The plain-text password to verify</param>
        /// <param name="storedHash">The base64-encoded "salt:hash" string stored in database</param>
        /// <returns>True if the password matches the stored hash; otherwise false</returns>
        /// <remarks>
        /// Uses constant-time comparison (FixedTimeEquals) to prevent timing attacks.
        /// Returns false if the stored hash format is invalid.
        /// </remarks>
        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                // Parse stored salt:hash format
                string[] parts = storedHash.Split(':');
                if (parts.Length != 2)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] storedHashBytes = Convert.FromBase64String(parts[1]);

                // Re-hash input password with extracted salt (same iterations)
                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                byte[] computedHash = pbkdf2.GetBytes(HashSize);

                // Use constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(storedHashBytes, computedHash);
            }
            catch
            {
                // Return false if hash format is invalid or parsing fails
                return false;
            }
        }
    }
}

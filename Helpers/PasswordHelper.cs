using System.Security.Cryptography;

namespace GYM.Helpers
{
    public static class PasswordHelper
    {
        // Generates a random 16-byte salt, Base64-encoded
        public static string GenerateSalt()
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToBase64String(saltBytes);
        }

        // Hashes a password with its salt using SHA-256
        public static string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var combined = System.Text.Encoding.UTF8.GetBytes(password + salt);
            var hashBytes = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hashBytes);
        }

        // Checks whether an input password matches the stored hash/salt
        public static bool VerifyPassword(string inputPassword, string storedHash, string storedSalt)
        {
            string inputHash = HashPassword(inputPassword, storedSalt);
            return inputHash == storedHash;
        }
    }
}
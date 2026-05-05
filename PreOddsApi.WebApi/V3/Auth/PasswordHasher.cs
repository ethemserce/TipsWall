using System;
using System.Security.Cryptography;

namespace PreOddsApi.WebApi.V3.Auth
{
    public static class PasswordHasher
    {
        public const string AlgorithmId = "pbkdf2-sha256-100000";
        public const string LegacyBridgeAlgorithmId = "legacy-prd_user-bridge";

        private const int IterationCount = 100_000;
        private const int SaltBytes = 32;
        private const int HashBytes = 32;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var hash = Derive(password, salt);
            return $"{AlgorithmId}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
                return false;

            var parts = storedValue.Split('$');
            if (parts.Length != 3 || parts[0] != AlgorithmId)
                return false;

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Derive(password, salt);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }

        private static byte[] Derive(string password, byte[] salt)
        {
            using var rfc = new Rfc2898DeriveBytes(
                password, salt, IterationCount, HashAlgorithmName.SHA256);
            return rfc.GetBytes(HashBytes);
        }
    }
}

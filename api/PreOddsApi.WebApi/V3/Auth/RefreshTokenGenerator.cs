using System;
using System.Security.Cryptography;
using System.Text;

namespace PreOddsApi.WebApi.V3.Auth
{
    public static class RefreshTokenGenerator
    {
        private const int RawTokenBytes = 32;

        public static string CreateRaw()
        {
            var bytes = RandomNumberGenerator.GetBytes(RawTokenBytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string Hash(string rawToken)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}

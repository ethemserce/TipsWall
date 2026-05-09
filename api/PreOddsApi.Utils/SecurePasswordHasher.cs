using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PreOddsApi.Utils
{
    public sealed class SecurePasswordHasher
    {
        public static string Hash(string password)
        {
            var data = Encoding.UTF8.GetBytes(password);
            using (SHA512 shaM = new SHA512Managed())
            {
                var hashedData = shaM.ComputeHash(data);
                var result = Encoding.UTF8.GetString(hashedData);
                return result;
            }
        }

        public static bool Verify(string password, string hashedPassword)
        {
            return Hash(password) == hashedPassword;
        }
    }
}

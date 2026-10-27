using System;
using System.Security.Cryptography;
using System.Text;

namespace HoneyErpAPI.Common
{
    public static class PasswordHasher
    {
        public static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            using (var hmac = new HMACSHA512(saltBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                passwordHash = Convert.ToBase64String(hashBytes);
                passwordSalt = Convert.ToBase64String(saltBytes);
            }
        }

        public static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedSalt))
                return false;

            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                using (var hmac = new HMACSHA512(saltBytes))
                {
                    byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                    string computedHashBase64 = Convert.ToBase64String(computedHash);

                    return CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(computedHashBase64),
                        Encoding.UTF8.GetBytes(storedHash)
                    );
                }
            }
            catch
            {
                // In case of non-base64 test salt, fallback to utf8 bytes
                try
                {
                    byte[] saltBytes = Encoding.UTF8.GetBytes(storedSalt);
                    using (var hmac = new HMACSHA512(saltBytes))
                    {
                        byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                        string computedHashBase64 = Convert.ToBase64String(computedHash);

                        return CryptographicOperations.FixedTimeEquals(
                            Encoding.UTF8.GetBytes(computedHashBase64),
                            Encoding.UTF8.GetBytes(storedHash)
                        );
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}

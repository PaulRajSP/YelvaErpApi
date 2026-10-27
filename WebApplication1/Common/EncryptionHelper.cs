using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HoneyErpAPI.Common
{
    public static class EncryptionHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("YelvaSoftwareBilYelvaSoftwareBil"); // 32 bytes

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.GenerateIV();

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    byte[] encrypted = msEncrypt.ToArray();

                    // IV + Encrypted Data
                    byte[] result = new byte[aesAlg.IV.Length + encrypted.Length];

                    Buffer.BlockCopy(aesAlg.IV, 0, result, 0, aesAlg.IV.Length);
                    Buffer.BlockCopy(encrypted, 0, result, aesAlg.IV.Length, encrypted.Length);

                    // URL-safe Base64
                    string base64 = Convert.ToBase64String(result);

                    return base64
                        .Replace('+', '-')
                        .Replace('/', '_')
                        .TrimEnd('=');
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                // Revert URL-safe Base64
                string base64 = cipherText
                    .Replace('-', '+')
                    .Replace('_', '/');

                switch (base64.Length % 4)
                {
                    case 2:
                        base64 += "==";
                        break;

                    case 3:
                        base64 += "=";
                        break;
                }

                byte[] cipherBytes = Convert.FromBase64String(base64);

                using (Aes aesAlg = Aes.Create())
                {
                    // First 16 bytes = IV
                    byte[] iv = new byte[16];
                    Buffer.BlockCopy(cipherBytes, 0, iv, 0, 16);

                    // Remaining bytes = encrypted data
                    byte[] encrypted = new byte[cipherBytes.Length - 16];
                    Buffer.BlockCopy(cipherBytes, 16, encrypted, 0, encrypted.Length);

                    aesAlg.Key = Key;
                    aesAlg.IV = iv;

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(encrypted))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public static T? Decrypt<T>(string cipherText)
        {
            string decryptedValue = Decrypt(cipherText);

            if (string.IsNullOrWhiteSpace(decryptedValue))
            {
                return default;
            }

            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            return (T?)Convert.ChangeType(decryptedValue, targetType);
        }

    }
}
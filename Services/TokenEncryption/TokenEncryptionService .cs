using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.TokenEncryption
{
    public sealed class TokenEncryptionService : ITokenEncryptionService
    {
        private readonly string _encryptionKey;

        public TokenEncryptionService(string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey))
                throw new ArgumentException("Encryption key cannot be null or empty", nameof(encryptionKey));

            // Ключ должен быть ровно 32 байта (256 бит) для AES-256
            var keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
            if (keyBytes.Length < 32)
                throw new ArgumentException("Encryption key must be at least 32 characters", nameof(encryptionKey));

            _encryptionKey = encryptionKey;
        }

        public string Encrypt(string plainToken)
        {
            if (string.IsNullOrWhiteSpace(plainToken))
                throw new ArgumentException("Token cannot be null or empty", nameof(plainToken));

            using (var aes = Aes.Create())
            {
                // Нормализуем ключ до 32 байт
                var keyBytes = new byte[32];
                var sourceKeyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
                Array.Copy(sourceKeyBytes, 0, keyBytes, 0, Math.Min(sourceKeyBytes.Length, 32));

                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // IV генерируется случайно для каждого шифрования
                aes.GenerateIV();
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (var ms = new System.IO.MemoryStream())
                {
                    // Пишем IV в начало зашифрованных данных
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new System.IO.StreamWriter(cs))
                    {
                        sw.Write(plainToken);
                    }

                    // Возвращаем IV + шифротекст в формате Base64
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string Decrypt(string encryptedToken)
        {
            if (string.IsNullOrWhiteSpace(encryptedToken))
                throw new ArgumentException("Encrypted token cannot be null or empty", nameof(encryptedToken));

            try
            {
                var buffer = Convert.FromBase64String(encryptedToken);

                using (var aes = Aes.Create())
                {
                    // Нормализуем ключ
                    var keyBytes = new byte[32];
                    var sourceKeyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
                    Array.Copy(sourceKeyBytes, 0, keyBytes, 0, Math.Min(sourceKeyBytes.Length, 32));

                    aes.Key = keyBytes;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Читаем IV из начала буфера
                    var iv = new byte[aes.IV.Length];
                    Array.Copy(buffer, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new System.IO.MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new System.IO.StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to decrypt token. The encryption key may be incorrect or the token may be corrupted.", ex);
            }
        }
    }
}


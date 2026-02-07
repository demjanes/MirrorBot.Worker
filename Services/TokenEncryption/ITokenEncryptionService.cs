using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.TokenEncryption
{
    public interface ITokenEncryptionService
    {
        /// <summary>
        /// Шифрует токен для безопасного хранения
        /// </summary>
        string Encrypt(string plainToken);

        /// <summary>
        /// Расшифровывает токен из БД
        /// </summary>
        string Decrypt(string encryptedToken);

        string ComputeTokenHash(string plainToken);
    }
}

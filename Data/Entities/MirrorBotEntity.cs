using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Entities
{
    public sealed class MirrorBotEntity : BaseEntity
    {
        public long OwnerTelegramUserId { get; set; }

        /// <summary>
        /// Шифрованный токен бота (никогда не открытый текст)
        /// </summary>
        [BsonElement("encryptedToken")]
        public string EncryptedToken { get; set; } = default!;
        [BsonElement("tokenHash")]
        public string TokenHash { get; set; } = default!;

        public string? BotUsername { get; set; }

        public bool IsEnabled { get; set; } = true;


        public DateTime? LastSeenAtUtc { get; set; }
        public string? LastError { get; set; }
    }
}

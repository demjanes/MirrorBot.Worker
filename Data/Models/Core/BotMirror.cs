using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Зеркало бота
    /// </summary>
    public sealed class BotMirror : BaseEntity
    {
        /// <summary>
        /// ID владельца бота в Telegram
        /// </summary>
        [BsonElement("ownerTelegramUserId")]
        public long OwnerTelegramUserId { get; set; }

        /// <summary>
        /// Шифрованный токен бота (никогда не открытый текст)
        /// </summary>
        [BsonElement("encryptedToken")]
        public string EncryptedToken { get; set; } = default!;

        /// <summary>
        /// Хеш токена для верификации
        /// </summary>
        [BsonElement("tokenHash")]
        public string TokenHash { get; set; } = default!;

        /// <summary>
        /// Username бота (без @)
        /// </summary>
        [BsonElement("botUsername")]
        public string? BotUsername { get; set; }

        /// <summary>
        /// Активен ли бот
        /// </summary>
        [BsonElement("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Последнее время активности бота
        /// </summary>
        [BsonElement("lastSeenAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastSeenAtUtc { get; set; }

        /// <summary>
        /// Последняя ошибка бота
        /// </summary>
        [BsonElement("lastError")]
        public string? LastError { get; set; }
    }
}

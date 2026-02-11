using MirrorBot.Worker.Data.Models.Core;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Models.Subscription
{
    /// <summary>
    /// Статистика использования ресурсов пользователем (за день).
    /// </summary>
    public sealed class UsageStats : BaseEntity
    {
        /// <summary>
        /// Telegram User ID.
        /// </summary>
        [BsonElement("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// Дата (YYYY-MM-DD) для ежедневной статистики.
        /// </summary>
        [BsonElement("date")]
        public string Date { get; set; } = string.Empty; // Формат: "2026-02-11"

        /// <summary>
        /// Количество отправленных текстовых сообщений.
        /// </summary>
        [BsonElement("textMessagesCount")]
        public int TextMessagesCount { get; set; }

        /// <summary>
        /// Количество отправленных голосовых сообщений.
        /// </summary>
        [BsonElement("voiceMessagesCount")]
        public int VoiceMessagesCount { get; set; }

        /// <summary>
        /// Общее количество использованных токенов AI.
        /// </summary>
        [BsonElement("totalTokensUsed")]
        public int TotalTokensUsed { get; set; }

        /// <summary>
        /// Дата последнего обновления.
        /// </summary>
        [BsonElement("updatedAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

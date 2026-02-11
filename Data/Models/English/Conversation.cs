using MirrorBot.Worker.Data.Models.Core;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Models.English
{
    /// <summary>
    /// Диалог с английским тьютором (единый для всех ботов пользователя)
    /// </summary>
    public sealed class Conversation : BaseEntity
    {
        /// <summary>
        /// ID пользователя Telegram
        /// </summary>
        [BsonElement("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// ID последнего бота, в котором пользователь писал
        /// </summary>
        [BsonElement("lastBotId")]
        public string LastBotId { get; set; } = string.Empty;

        /// <summary>
        /// Режим диалога (Casual, Business, Psychologist, Teacher)
        /// </summary>
        [BsonElement("mode")]
        public string Mode { get; set; } = "Casual";

        /// <summary>
        /// Сообщения в диалоге (глобальные для всех ботов пользователя)
        /// </summary>
        [BsonElement("messages")]
        public List<EnglishMessage> Messages { get; set; } = new();

        /// <summary>
        /// Последняя активность
        /// </summary>
        [BsonElement("lastActivityUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Активный ли диалог
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Общее количество токенов использовано в этом диалоге
        /// </summary>
        [BsonElement("totalTokensUsed")]
        public int TotalTokensUsed { get; set; } = 0;
    }

}

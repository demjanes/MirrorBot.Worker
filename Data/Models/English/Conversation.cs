using MirrorBot.Worker.Data.Models.Core;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.English
{
    /// <summary>
    /// Диалог с английским тьютором
    /// </summary>
    public sealed class Conversation : BaseEntity
    {
        /// <summary>
        /// ID пользователя Telegram
        /// </summary>
        [BsonElement("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// ID бота (для системы зеркал)
        /// </summary>
        [BsonElement("botId")]
        public string BotId { get; set; } = string.Empty;

        /// <summary>
        /// Режим диалога (Casual, Business, Psychologist, Teacher)
        /// </summary>
        [BsonElement("mode")]
        public string Mode { get; set; } = "Casual";

        /// <summary>
        /// Сообщения в диалоге
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

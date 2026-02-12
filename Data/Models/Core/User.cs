using MirrorBot.Worker.Flow.UI.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Пользователь системы
    /// </summary>
    public sealed class User : BaseEntity
    {
        /// <summary>
        /// ID пользователя в Telegram
        /// </summary>
        [BsonElement("tgUserId")]
        public long TgUserId { get; set; }

        /// <summary>
        /// Username в Telegram (без @)
        /// </summary>
        [BsonElement("tgUsername")]
        public string? TgUsername { get; set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        [BsonElement("tgFirstName")]
        public string? TgFirstName { get; set; }

        /// <summary>
        /// Фамилия пользователя
        /// </summary>
        [BsonElement("tgLastName")]
        public string? TgLastName { get; set; }

        /// <summary>
        /// Предпочитаемый язык интерфейса
        /// </summary>
        [BsonElement("preferredLang")]
        [BsonRepresentation(BsonType.String)]
        public UiLang PreferredLang { get; set; } = UiLang.Def;

        /// <summary>
        /// Код языка из Telegram (например "ru-RU")
        /// </summary>
        [BsonElement("tgLangCode")]
        public string? TgLangCode { get; set; }

        /// <summary>
        /// Ключ последнего использованного бота (например "__main__" или mirrorBotId)
        /// </summary>
        [BsonElement("lastBotKey")]
        public string? LastBotKey { get; set; }

        /// <summary>
        /// ID последнего чата
        /// </summary>
        [BsonElement("lastChatId")]
        public long? LastChatId { get; set; }

        /// <summary>
        /// Время последнего сообщения
        /// </summary>
        [BsonElement("lastMessageAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastMessageAtUtc { get; set; }

        /// <summary>
        /// Может ли отправлять сообщения через последнего бота
        /// </summary>
        [BsonElement("canSendLastBot")]
        public bool CanSendLastBot { get; set; } = true;

        /// <summary>
        /// Последняя ошибка отправки
        /// </summary>
        [BsonElement("lastSendError")]
        public string? LastSendError { get; set; }

        /// <summary>
        /// ID владельца бота-реферера (кто пригласил)
        /// </summary>
        [BsonElement("referrerOwnerTgUserId")]
        public long? ReferrerOwnerTgUserId { get; set; }

        /// <summary>
        /// ID бота-реферера
        /// </summary>
        [BsonElement("referrerMirrorBotId")]
        public ObjectId? ReferrerMirrorBotId { get; set; }
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Базовая сущность для всех моделей
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Уникальный идентификатор
        /// </summary>
        [BsonId]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Дата создания (UTC)
        /// </summary>
        [BsonElement("createdAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

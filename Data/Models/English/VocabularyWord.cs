using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.English
{
    /// <summary>
    /// Слово в словаре пользователя
    /// </summary>
    public sealed class VocabularyWord
    {
        /// <summary>
        /// Английское слово
        /// </summary>
        [BsonElement("word")]
        public string Word { get; set; } = string.Empty;

        /// <summary>
        /// Перевод на русский
        /// </summary>
        [BsonElement("translation")]
        public string Translation { get; set; } = string.Empty;

        /// <summary>
        /// Контекст использования (предложение)
        /// </summary>
        [BsonElement("context")]
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Дата добавления
        /// </summary>
        [BsonElement("addedAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Количество повторений
        /// </summary>
        [BsonElement("reviewCount")]
        public int ReviewCount { get; set; } = 0;

        /// <summary>
        /// Дата последнего повторения
        /// </summary>
        [BsonElement("lastReviewUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastReviewUtc { get; set; }

        /// <summary>
        /// Выучено ли слово (5+ успешных повторений)
        /// </summary>
        [BsonElement("isLearned")]
        public bool IsLearned { get; set; } = false;

        /// <summary>
        /// Сложность слова (A1, A2, B1, B2, C1, C2)
        /// </summary>
        [BsonElement("level")]
        public string? Level { get; set; }
    }
}

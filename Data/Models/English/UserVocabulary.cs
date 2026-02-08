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
    /// Словарь пользователя
    /// </summary>
    public sealed class UserVocabulary : BaseEntity
    {
        /// <summary>
        /// ID пользователя Telegram
        /// </summary>
        [BsonElement("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// Слова в словаре
        /// </summary>
        [BsonElement("words")]
        public List<VocabularyWord> Words { get; set; } = new();

        /// <summary>
        /// Последнее обновление
        /// </summary>
        [BsonElement("updatedAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

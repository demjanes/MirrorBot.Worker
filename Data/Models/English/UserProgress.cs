using MirrorBot.Worker.Data.Models.Core;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.English
{
    // <summary>
    /// Прогресс пользователя в изучении английского
    /// </summary>
    public sealed class UserProgress : BaseEntity
    {
        /// <summary>
        /// ID пользователя Telegram
        /// </summary>
        [BsonElement("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// Текущий уровень (A1, A2, B1, B2, C1, C2)
        /// </summary>
        [BsonElement("currentLevel")]
        public string CurrentLevel { get; set; } = "A1";

        /// <summary>
        /// Общее количество сообщений
        /// </summary>
        [BsonElement("totalMessages")]
        public int TotalMessages { get; set; } = 0;

        /// <summary>
        /// Количество голосовых сообщений
        /// </summary>
        [BsonElement("voiceMessages")]
        public int VoiceMessages { get; set; } = 0;

        /// <summary>
        /// Общее количество исправлений
        /// </summary>
        [BsonElement("totalCorrections")]
        public int TotalCorrections { get; set; } = 0;

        /// <summary>
        /// Размер словаря
        /// </summary>
        [BsonElement("vocabularySize")]
        public int VocabularySize { get; set; } = 0;

        /// <summary>
        /// Последняя активность
        /// </summary>
        [BsonElement("lastActivityUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Средняя оценка произношения
        /// </summary>
        [BsonElement("avgPronunciationScore")]
        public double AvgPronunciationScore { get; set; } = 0;

        /// <summary>
        /// Streak (дни подряд)
        /// </summary>
        [BsonElement("currentStreak")]
        public int CurrentStreak { get; set; } = 0;

        /// <summary>
        /// Лучший streak
        /// </summary>
        [BsonElement("bestStreak")]
        public int BestStreak { get; set; } = 0;
    }
}

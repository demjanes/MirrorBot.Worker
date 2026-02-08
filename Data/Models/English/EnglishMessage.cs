using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.English
{
    /// <summary>
    /// Сообщение в диалоге
    /// </summary>
    public sealed class EnglishMessage
    {
        /// <summary>
        /// Роль: user, assistant, system
        /// </summary>
        [BsonElement("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Текст сообщения
        /// </summary>
        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Telegram File ID (для голосовых сообщений)
        /// </summary>
        [BsonElement("voiceFileId")]
        public string? VoiceFileId { get; set; }

        /// <summary>
        /// Исправления грамматики
        /// </summary>
        [BsonElement("corrections")]
        public List<MessageCorrection> Corrections { get; set; } = new();

        /// <summary>
        /// Timestamp сообщения
        /// </summary>
        [BsonElement("timestampUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Токены использованные для генерации ответа
        /// </summary>
        [BsonElement("tokensUsed")]
        public int TokensUsed { get; set; } = 0;

        /// <summary>
        /// Оценка произношения (для голосовых)
        /// </summary>
        [BsonElement("pronunciationScore")]
        public int? PronunciationScore { get; set; }
    }
}

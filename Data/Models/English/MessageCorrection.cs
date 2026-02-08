using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.English
{
    /// <summary>
    /// Грамматическое исправление
    /// </summary>
    public sealed class MessageCorrection
    {
        /// <summary>
        /// Оригинальный текст с ошибкой
        /// </summary>
        [BsonElement("original")]
        public string Original { get; set; } = string.Empty;

        /// <summary>
        /// Исправленный текст
        /// </summary>
        [BsonElement("corrected")]
        public string Corrected { get; set; } = string.Empty;

        /// <summary>
        /// Объяснение ошибки
        /// </summary>
        [BsonElement("explanation")]
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// Тип ошибки: grammar, spelling, style, vocabulary
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;
    }
}

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
    /// Настройки пользователя для английского тьютора
    /// </summary>
    public sealed class UserSettings : BaseEntity
    {
        /// <summary>
        /// ID пользователя Telegram
        /// </summary>
        [BsonElement("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// Предпочитаемый голос для TTS (alena, jane, omazh для Yandex)
        /// </summary>
        [BsonElement("preferredVoice")]
        public string PreferredVoice { get; set; } = "jane";

        /// <summary>
        /// Скорость речи (0.5 - 2.0)
        /// </summary>
        [BsonElement("speechSpeed")]
        public double SpeechSpeed { get; set; } = 1.0;

        /// <summary>
        /// Автоматически отправлять голосовые ответы
        /// </summary>
        [BsonElement("autoVoiceResponse")]
        public bool AutoVoiceResponse { get; set; } = true;

        /// <summary>
        /// Показывать исправления грамматики
        /// </summary>
        [BsonElement("showCorrections")]
        public bool ShowCorrections { get; set; } = true;

        /// <summary>
        /// Уровень детализации исправлений (Simple, Detailed, Full)
        /// </summary>
        [BsonElement("correctionLevel")]
        public string CorrectionLevel { get; set; } = "Detailed";

        /// <summary>
        /// Автоматически добавлять новые слова в словарь
        /// </summary>
        [BsonElement("autoAddToVocabulary")]
        public bool AutoAddToVocabulary { get; set; } = true;

        /// <summary>
        /// Режим диалога по умолчанию
        /// </summary>
        [BsonElement("defaultMode")]
        public string DefaultMode { get; set; } = "Casual";

        /// <summary>
        /// Уведомления о ежедневных целях
        /// </summary>
        [BsonElement("dailyReminders")]
        public bool DailyReminders { get; set; } = false;

        /// <summary>
        /// Время напоминаний (UTC)
        /// </summary>
        [BsonElement("reminderTimeUtc")]
        public TimeSpan? ReminderTimeUtc { get; set; }
    }
}

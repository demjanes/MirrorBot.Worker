using MirrorBot.Worker.Data.Models.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Models.Subscription
{
    /// <summary>
    /// Тарифный план с параметрами и лимитами.
    /// </summary>
    public sealed class SubscriptionPlan : BaseEntity
    {
        /// <summary>
        /// Тип подписки (Free, PremiumMonthly, и т.д.)
        /// </summary>
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public SubscriptionType Type { get; set; }

        /// <summary>
        /// Название тарифа (отображается пользователю).
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Описание тарифа.
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Цена в рублях.
        /// </summary>
        [BsonElement("priceRub")]
        public decimal PriceRub { get; set; }

        /// <summary>
        /// Цена в долларах.
        /// </summary>
        [BsonElement("priceUsd")]
        public decimal PriceUsd { get; set; }

        /// <summary>
        /// Длительность подписки в днях.
        /// </summary>
        [BsonElement("durationDays")]
        public int DurationDays { get; set; }

        /// <summary>
        /// Лимит текстовых сообщений в день (-1 = безлимит).
        /// </summary>
        [BsonElement("dailyTextMessageLimit")]
        public int DailyTextMessageLimit { get; set; }

        /// <summary>
        /// Лимит голосовых сообщений в день (-1 = безлимит).
        /// </summary>
        [BsonElement("dailyVoiceMessageLimit")]
        public int DailyVoiceMessageLimit { get; set; }

        /// <summary>
        /// Максимальное количество токенов на запрос к AI.
        /// </summary>
        [BsonElement("maxTokensPerRequest")]
        public int MaxTokensPerRequest { get; set; }

        /// <summary>
        /// Доступность голосового ответа (TTS).
        /// </summary>
        [BsonElement("voiceResponseEnabled")]
        public bool VoiceResponseEnabled { get; set; }

        /// <summary>
        /// Доступность грамматических исправлений.
        /// </summary>
        [BsonElement("grammarCorrectionEnabled")]
        public bool GrammarCorrectionEnabled { get; set; }

        /// <summary>
        /// Доступность словаря новых слов.
        /// </summary>
        [BsonElement("vocabularyTrackingEnabled")]
        public bool VocabularyTrackingEnabled { get; set; }

        /// <summary>
        /// Приоритетная обработка (быстрее ответ).
        /// </summary>
        [BsonElement("priorityProcessing")]
        public bool PriorityProcessing { get; set; }

        /// <summary>
        /// Активен ли этот тарифный план.
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}

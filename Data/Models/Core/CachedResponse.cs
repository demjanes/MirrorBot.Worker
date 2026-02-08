using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Кэшированный ответ ИИ на пользовательский запрос.
    /// </summary>
    public class CachedResponse : BaseEntity
    {
        /// <summary>
        /// Ключ кэша (хеш от вопроса, режима диалога, контекста и модели).
        /// </summary>
        [BsonElement("cacheKey")]
        public string CacheKey { get; set; } = null!;

        /// <summary>
        /// Исходный текст вопроса пользователя (после расшифровки голосового, если было).
        /// </summary>
        [BsonElement("question")]
        public string Question { get; set; } = null!;

        /// <summary>
        /// Режим диалога (например, general, grammar, vocabulary и т.п.).
        /// </summary>
        [BsonElement("dialogMode")]
        public string DialogMode { get; set; } = null!;

        /// <summary>
        /// Хеш контекста (история диалога, настройки пользователя и т.п.).
        /// </summary>
        [BsonElement("contextHash")]
        public string ContextHash { get; set; } = null!;

        /// <summary>
        /// Идентификатор модели (YandexGPT, OpenAI и т.д.), опционально.
        /// </summary>
        [BsonElement("modelId")]
        public string? ModelId { get; set; }

        /// <summary>
        /// Текстовый ответ модели.
        /// </summary>
        [BsonElement("responseText")]
        public string ResponseText { get; set; } = null!;

        /// <summary>
        /// Telegram FileId уже сгенерированного голосового ответа (если есть).
        /// </summary>
        [BsonElement("voiceFileId")]
        public string? VoiceFileId { get; set; }

        /// <summary>
        /// Количество токенов, использованных при генерации ответа.
        /// </summary>
        [BsonElement("tokensUsed")]
        public int TokensUsed { get; set; }

        /// <summary>
        /// Анализ произношения (для голосовых сообщений).
        /// </summary>
        [BsonElement("pronunciationAnalysis")]
        public CachedPronunciationAnalysis? PronunciationAnalysis { get; set; }

        /// <summary>
        /// Количество раз, когда этот кэш был использован (hit count).
        /// </summary>
        [BsonElement("hitCount")]
        public int HitCount { get; set; }

        /// <summary>
        /// Дата последнего использования кэша (UTC).
        /// </summary>
        [BsonElement("lastUsedAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastUsedAtUtc { get; set; }

        /// <summary>
        /// Момент истечения срока действия записи (UTC).
        /// Используется TTL-индексом MongoDB для автоудаления документов.
        /// </summary>
        [BsonElement("expiresAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ExpiresAtUtc { get; set; }
    }

    /// <summary>
    /// Анализ произношения для кэширования (соответствует PronunciationAnalysis).
    /// </summary>
    public class CachedPronunciationAnalysis
    {
        /// <summary>
        /// Общая оценка (0-100).
        /// </summary>
        [BsonElement("score")]
        public int Score { get; set; }

        /// <summary>
        /// Детали по словам.
        /// </summary>
        [BsonElement("words")]
        public List<CachedWordPronunciation> Words { get; set; } = new();
    }

    /// <summary>
    /// Произношение отдельного слова для кэширования (соответствует WordPronunciation).
    /// </summary>
    public class CachedWordPronunciation
    {
        [BsonElement("word")]
        public string Word { get; set; } = string.Empty;

        [BsonElement("score")]
        public int Score { get; set; }

        [BsonElement("feedback")]
        public string? Feedback { get; set; }
    }
}

using MirrorBot.Worker.Data.Models.Core;

namespace MirrorBot.Worker.Services.AI.Interfaces
{
    /// <summary>
    /// Высокоуровневый сервис кэширования ответов ИИ.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Получить кэшированный ответ, если он есть, и инкрементировать счетчик использования.
        /// </summary>
        Task<CachedResponse?> GetAsync(
            string question,
            string dialogMode,
            string contextHash,
            string? modelId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохранить ответ в кэш (для текстовых сообщений).
        /// </summary>
        Task SaveAsync(
            string question,
            string dialogMode,
            string contextHash,
            string? modelId,
            string responseText,
            string? voiceFileId,
            int tokensUsed,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохранить ответ в кэш с данными произношения (для голосовых сообщений).
        /// </summary>
        Task SaveWithPronunciationAsync(
            string question,
            string dialogMode,
            string contextHash,
            string? modelId,
            string responseText,
            string? voiceFileId,
            int tokensUsed,
            PronunciationAnalysis? pronunciationAnalysis,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить voiceFileId для существующего кэша (после отправки в Telegram).
        /// </summary>
        Task UpdateVoiceFileIdAsync(
            string cacheKey,
            string voiceFileId,
            CancellationToken cancellationToken = default);
    }
}

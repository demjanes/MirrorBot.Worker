using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Services.AI.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MirrorBot.Worker.Services.AI.Implementations
{
    /// <summary>
    /// Реализация сервиса кэширования ответов ИИ.
    /// </summary>
    public class CacheService : ICacheService
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(1);

        private readonly ICacheRepository _cacheRepository;

        public CacheService(ICacheRepository cacheRepository)
        {
            _cacheRepository = cacheRepository;
        }

        public async Task<CachedResponse?> GetAsync(
            string question,
            string dialogMode,
            string contextHash,
            string? modelId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(question, dialogMode, contextHash, modelId);
            var cached = await _cacheRepository.GetByCacheKeyAsync(cacheKey, cancellationToken);

            if (cached != null)
            {
                // Инкрементируем счетчик использования
                cached.HitCount++;
                cached.LastUsedAtUtc = DateTime.UtcNow;

                await _cacheRepository.SaveAsync(cached, cancellationToken);
            }

            return cached;
        }

        public async Task SaveAsync(
            string question,
            string dialogMode,
            string contextHash,
            string? modelId,
            string responseText,
            string? voiceFileId,
            int tokensUsed,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var entity = new CachedResponse
            {
                CacheKey = GenerateCacheKey(question, dialogMode, contextHash, modelId),
                Question = question,
                DialogMode = dialogMode,
                ContextHash = contextHash,
                ModelId = modelId,
                ResponseText = responseText,
                VoiceFileId = voiceFileId,
                TokensUsed = tokensUsed,
                PronunciationAnalysis = null,
                HitCount = 0,
                CreatedAtUtc = now,
                LastUsedAtUtc = null,
                ExpiresAtUtc = now.Add(CacheTtl)
            };

            await _cacheRepository.SaveAsync(entity, cancellationToken);
        }

        public async Task SaveWithPronunciationAsync(
            string question,
            string dialogMode,
            string contextHash,
            string? modelId,
            string responseText,
            string? voiceFileId,
            int tokensUsed,
            PronunciationAnalysis? pronunciationAnalysis,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var entity = new CachedResponse
            {
                CacheKey = GenerateCacheKey(question, dialogMode, contextHash, modelId),
                Question = question,
                DialogMode = dialogMode,
                ContextHash = contextHash,
                ModelId = modelId,
                ResponseText = responseText,
                VoiceFileId = voiceFileId,
                TokensUsed = tokensUsed,
                PronunciationAnalysis = ConvertToCachedPronunciation(pronunciationAnalysis),
                HitCount = 0,
                CreatedAtUtc = now,
                LastUsedAtUtc = null,
                ExpiresAtUtc = now.Add(CacheTtl)
            };

            await _cacheRepository.SaveAsync(entity, cancellationToken);
        }

        public async Task UpdateVoiceFileIdAsync(
            string cacheKey,
            string voiceFileId,
            CancellationToken cancellationToken = default)
        {
            var cached = await _cacheRepository.GetByCacheKeyAsync(cacheKey, cancellationToken);

            if (cached != null)
            {
                cached.VoiceFileId = voiceFileId;
                await _cacheRepository.SaveAsync(cached, cancellationToken);
            }
        }

        /// <summary>
        /// Конвертировать PronunciationAnalysis в CachedPronunciationAnalysis для сохранения.
        /// </summary>
        private static CachedPronunciationAnalysis? ConvertToCachedPronunciation(
            PronunciationAnalysis? pronunciation)
        {
            if (pronunciation == null)
                return null;

            return new CachedPronunciationAnalysis
            {
                Score = pronunciation.Score,
                Words = pronunciation.Words.Select(w => new CachedWordPronunciation
                {
                    Word = w.Word,
                    Score = w.Score,
                    Feedback = w.Feedback
                }).ToList()
            };
        }

        /// <summary>
        /// Конвертировать CachedPronunciationAnalysis обратно в PronunciationAnalysis.
        /// </summary>
        public static PronunciationAnalysis? ConvertFromCachedPronunciation(
            CachedPronunciationAnalysis? cached)
        {
            if (cached == null)
                return null;

            return new PronunciationAnalysis
            {
                Score = cached.Score,
                Words = cached.Words.Select(w => new WordPronunciation
                {
                    Word = w.Word,
                    Score = w.Score,
                    Feedback = w.Feedback
                }).ToList()
            };
        }

        private static string GenerateCacheKey(
            string question,
            string dialogMode,
            string contextHash,
            string? modelId)
        {
            var raw = $"{dialogMode}||{modelId ?? string.Empty}||{contextHash}||{question}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }
    }
}

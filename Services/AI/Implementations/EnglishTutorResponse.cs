using MirrorBot.Worker.Services.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Implementations
{
    public class EnglishTutorResponse
    {
        public string TextResponse { get; set; } = null!;
        public byte[]? VoiceResponse { get; set; }

        /// <summary>
        /// Кэшированный Telegram FileId голосового сообщения (если есть).
        /// </summary>
        public string? CachedVoiceFileId { get; set; }

        /// <summary>
        /// Ключ кэша для обновления voiceFileId после отправки.
        /// </summary>
        public string? CacheKey { get; set; }

        public List<GrammarCorrection> Corrections { get; set; } = new();
        public List<string> NewVocabulary { get; set; } = new();

        /// <summary>
        /// Анализ произношения (для голосовых сообщений).
        /// </summary>
        public PronunciationAnalysis? PronunciationFeedback { get; set; }

        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

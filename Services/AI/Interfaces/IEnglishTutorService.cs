using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Interfaces
{
    /// <summary>
    /// Сервис для работы с английским тьютором
    /// </summary>
    public interface IEnglishTutorService
    {
        /// <summary>
        /// Обработка текстового сообщения от пользователя
        /// </summary>
        Task<EnglishTutorResponse> ProcessTextMessageAsync(
            long userId,
            string botId,
            string userMessage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обработка голосового сообщения от пользователя
        /// </summary>
        Task<EnglishTutorResponse> ProcessVoiceMessageAsync(
            long userId,
            string botId,
            byte[] audioData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Смена режима диалога
        /// </summary>
        Task SetConversationModeAsync(
            long userId,
            string botId,
            ConversationMode mode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получение статистики пользователя
        /// </summary>
        Task<UserStatistics> GetUserStatisticsAsync(
            long userId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Ответ от тьютора
    /// </summary>
    public class EnglishTutorResponse
    {
        /// <summary>
        /// Текстовый ответ
        /// </summary>
        public string TextResponse { get; set; } = string.Empty;

        /// <summary>
        /// Голосовой ответ (если есть)
        /// </summary>
        public byte[]? VoiceResponse { get; set; }

        /// <summary>
        /// Исправления грамматики
        /// </summary>
        public List<GrammarCorrection> Corrections { get; set; } = new();

        /// <summary>
        /// Новые слова для словаря
        /// </summary>
        public List<string> NewVocabulary { get; set; } = new();

        /// <summary>
        /// Анализ произношения (для голосовых)
        /// </summary>
        public PronunciationAnalysis? PronunciationFeedback { get; set; }

        /// <summary>
        /// Успешность операции
        /// </summary>
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Исправление грамматики
    /// </summary>
    public class GrammarCorrection
    {
        public string Original { get; set; } = string.Empty;
        public string Corrected { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // grammar, spelling, style
    }

    /// <summary>
    /// Режим диалога
    /// </summary>
    public enum ConversationMode
    {
        Casual,      // Повседневный
        Business,    // Деловой
        Psychologist, // Психолог
        Teacher      // Преподаватель (строгий)
    }

    /// <summary>
    /// Статистика пользователя
    /// </summary>
    public class UserStatistics
    {
        public int TotalMessages { get; set; }
        public int VocabularySize { get; set; }
        public int CorrectionsCount { get; set; }
        public DateTime LastActivity { get; set; }
        public string CurrentLevel { get; set; } = "A1"; // A1, A2, B1, B2, C1, C2
    }
}

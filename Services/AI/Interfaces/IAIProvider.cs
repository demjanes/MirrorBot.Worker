using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Interfaces
{
    /// <summary>
    /// Интерфейс для AI-провайдера (YandexGPT, OpenAI, etc.)
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Генерация ответа на основе промпта и истории диалога
        /// </summary>
        Task<AIResponse> GenerateResponseAsync(
            AIRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Название провайдера
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Проверка доступности сервиса
        /// </summary>
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Запрос к AI
    /// </summary>
    public class AIRequest
    {
        /// <summary>
        /// Системный промпт (роль AI)
        /// </summary>
        public string SystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// История сообщений
        /// </summary>
        public List<ChatMessage> Messages { get; set; } = new();

        /// <summary>
        /// Температура (креативность) 0.0 - 1.0
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Максимум токенов в ответе
        /// </summary>
        public int MaxTokens { get; set; } = 1000;
    }

    /// <summary>
    /// Сообщение в чате
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Ответ от AI
    /// </summary>
    public class AIResponse
    {
        public string Content { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public string Model { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

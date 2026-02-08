using MirrorBot.Worker.Services.AI.Interfaces;
using MirrorBot.Worker.Services.English.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.English
{
    /// <summary>
    /// Сервис для извлечения новых слов из диалога
    /// </summary>
    public class VocabularyExtractor
    {
        private readonly IAIProvider _aiProvider;
        private readonly ILogger<VocabularyExtractor> _logger;

        public VocabularyExtractor(IAIProvider aiProvider, ILogger<VocabularyExtractor> logger)
        {
            _aiProvider = aiProvider;
            _logger = logger;
        }

        /// <summary>
        /// Извлечь новые слова из текста
        /// </summary>
        public async Task<List<VocabularyWordDto>> ExtractAsync(
            string userText,
            string assistantText,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var conversation = $"User: {userText}\nAssistant: {assistantText}";

                var request = new AIRequest
                {
                    SystemPrompt = "You are a vocabulary extractor. Always respond with valid JSON only.",
                    Messages = new List<ChatMessage>
                {
                    new()
                    {
                        Role = "user",
                        Content = EnglishTutorPrompts.VocabularyExtraction + conversation
                    }
                },
                    Temperature = 0.3,
                    MaxTokens = 800
                };

                var response = await _aiProvider.GenerateResponseAsync(request, cancellationToken);

                if (!response.Success)
                {
                    _logger.LogWarning("Vocabulary extraction failed: {Error}", response.ErrorMessage);
                    return new List<VocabularyWordDto>();
                }

                var jsonContent = ExtractJson(response.Content);

                var words = JsonSerializer.Deserialize<List<VocabularyWordDto>>(
                    jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return words ?? new List<VocabularyWordDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting vocabulary");
                return new List<VocabularyWordDto>();
            }
        }

        private string ExtractJson(string content)
        {
            if (content.Contains("```json"))
            {
                var start = content.IndexOf("```json") + 7;
                var end = content.IndexOf("```", start);
                if (end > start)
                    return content.Substring(start, end - start).Trim();
            }
            else if (content.Contains("```"))
            {
                var start = content.IndexOf("```") + 3;
                var end = content.IndexOf("```", start);
                if (end > start)
                    return content.Substring(start, end - start).Trim();
            }

            var arrayStart = content.IndexOf('[');
            var arrayEnd = content.LastIndexOf(']');
            if (arrayStart >= 0 && arrayEnd > arrayStart)
                return content.Substring(arrayStart, arrayEnd - arrayStart + 1);

            return content;
        }
    }

    /// <summary>
    /// DTO для слова из словаря
    /// </summary>
    public class VocabularyWordDto
    {
        public string Word { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
    }
}

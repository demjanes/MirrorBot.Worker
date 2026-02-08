using MirrorBot.Worker.Services.AI.Interfaces;
using MirrorBot.Worker.Services.English.Prompts;
using System.Text.Json;

namespace MirrorBot.Worker.Services.English
{
    /// <summary>
    /// Сервис для анализа грамматики
    /// </summary>
    public class GrammarAnalyzer
    {
        private readonly IAIProvider _aiProvider;
        private readonly ILogger<GrammarAnalyzer> _logger;

        public GrammarAnalyzer(IAIProvider aiProvider, ILogger<GrammarAnalyzer> logger)
        {
            _aiProvider = aiProvider;
            _logger = logger;
        }

        /// <summary>
        /// Анализировать текст и найти грамматические ошибки
        /// </summary>
        public async Task<List<GrammarCorrection>> AnalyzeAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return new List<GrammarCorrection>();

                var request = new AIRequest
                {
                    SystemPrompt = "You are a grammar checker. Always respond with valid JSON only.",
                    Messages = new List<ChatMessage>
                {
                    new()
                    {
                        Role = "user",
                        Content = EnglishTutorPrompts.GrammarAnalysis + text
                    }
                },
                    Temperature = 0.3,
                    MaxTokens = 1000
                };

                var response = await _aiProvider.GenerateResponseAsync(request, cancellationToken);

                if (!response.Success)
                {
                    _logger.LogWarning("Grammar analysis failed: {Error}", response.ErrorMessage);
                    return new List<GrammarCorrection>();
                }

                var jsonContent = ExtractJson(response.Content);

                var corrections = JsonSerializer.Deserialize<List<GrammarCorrectionDto>>(
                    jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // ✅ ИСПРАВЛЕНО: используем GrammarCorrection из Interfaces
                return corrections?.Select(c => new GrammarCorrection
                {
                    Original = c.Original,
                    Corrected = c.Corrected,
                    Explanation = c.Explanation,
                    Type = c.Type
                }).ToList() ?? new List<GrammarCorrection>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing grammar");
                return new List<GrammarCorrection>();
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

        // ✅ Приватный DTO только для десериализации JSON
        private class GrammarCorrectionDto
        {
            public string Original { get; set; } = string.Empty;
            public string Corrected { get; set; } = string.Empty;
            public string Explanation { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }
    }


}

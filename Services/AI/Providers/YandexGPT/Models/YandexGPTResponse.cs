using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Providers.YandexGPT.Models
{
    /// <summary>
    /// Ответ от YandexGPT API
    /// </summary>
    public class YandexGPTResponse
    {
        [JsonPropertyName("result")]
        public YandexResult Result { get; set; } = new();
    }

    public class YandexResult
    {
        [JsonPropertyName("alternatives")]
        public List<YandexAlternative> Alternatives { get; set; } = new();

        [JsonPropertyName("usage")]
        public YandexUsage Usage { get; set; } = new();

        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; } = string.Empty;
    }

    public class YandexAlternative
    {
        [JsonPropertyName("message")]
        public YandexMessage Message { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class YandexUsage
    {
        // ✅ ИСПРАВЛЕНО: Яндекс возвращает токены как строки!
        [JsonPropertyName("inputTextTokens")]
        public string InputTextTokens { get; set; } = "0";

        [JsonPropertyName("completionTokens")]
        public string CompletionTokens { get; set; } = "0";

        [JsonPropertyName("totalTokens")]
        public string TotalTokens { get; set; } = "0";

        // Вспомогательные свойства для преобразования в int
        [JsonIgnore]
        public int InputTextTokensInt => int.TryParse(InputTextTokens, out var val) ? val : 0;

        [JsonIgnore]
        public int CompletionTokensInt => int.TryParse(CompletionTokens, out var val) ? val : 0;

        [JsonIgnore]
        public int TotalTokensInt => int.TryParse(TotalTokens, out var val) ? val : 0;
    }
}

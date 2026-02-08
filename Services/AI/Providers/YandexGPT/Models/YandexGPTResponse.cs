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
        [JsonPropertyName("inputTextTokens")]
        public int InputTextTokens { get; set; }

        [JsonPropertyName("completionTokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; set; }
    }
}

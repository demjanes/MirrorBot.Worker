using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Providers.YandexGPT.Models
{
    /// <summary>
    /// Запрос к YandexGPT API
    /// </summary>
    public class YandexGPTRequest
    {
        [JsonPropertyName("modelUri")]
        public string ModelUri { get; set; } = string.Empty;

        [JsonPropertyName("completionOptions")]
        public CompletionOptions CompletionOptions { get; set; } = new();

        [JsonPropertyName("messages")]
        public List<YandexMessage> Messages { get; set; } = new();
    }

    public class CompletionOptions
    {
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; } = 2000;
    }

    public class YandexMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}

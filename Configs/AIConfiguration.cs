namespace MirrorBot.Worker.Configs
{
    /// <summary>
    /// Конфигурация AI сервисов
    /// </summary>
    public class AIConfiguration
    {
        public const string SectionName = "AI";

        /// <summary>
        /// Активный провайдер (YandexGPT, OpenAI)
        /// </summary>
        public string Provider { get; set; } = "YandexGPT";

        public YandexGPTConfig YandexGPT { get; set; } = new();
        public OpenAIConfig OpenAI { get; set; } = new();
    }

    public class YandexGPTConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FolderId { get; set; } = string.Empty;
        public string Model { get; set; } = "yandexgpt/latest";
        public string BaseUrl { get; set; } = "https://llm.api.cloud.yandex.net/foundationModels/v1";
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
    }

    public class OpenAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
    }
}

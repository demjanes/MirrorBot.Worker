using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Services.AI.Interfaces;
using MirrorBot.Worker.Services.AI.Providers.YandexGPT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Providers.YandexGPT
{

    /// <summary>
    /// Провайдер для работы с YandexGPT API
    /// </summary>
    public class YandexGPTProvider : IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly YandexGPTConfig _config;
        private readonly ILogger<YandexGPTProvider> _logger;

        public string ProviderName => "YandexGPT";

        public YandexGPTProvider(
            HttpClient httpClient,
            IOptions<AIConfiguration> aiConfig,
            ILogger<YandexGPTProvider> logger)
        {
            _httpClient = httpClient;
            _config = aiConfig.Value.YandexGPT;
            _logger = logger;

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Api-Key", _config.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("x-folder-id", _config.FolderId);
        }

        public async Task<AIResponse> GenerateResponseAsync(
            AIRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating response using YandexGPT");

                var yandexRequest = MapToYandexRequest(request);
                var jsonContent = JsonSerializer.Serialize(yandexRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "/completion",
                    content,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("YandexGPT API error: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    return new AIResponse
                    {
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode}"
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var yandexResponse = JsonSerializer.Deserialize<YandexGPTResponse>(responseJson);

                if (yandexResponse?.Result?.Alternatives == null ||
                    yandexResponse.Result.Alternatives.Count == 0)
                {
                    return new AIResponse
                    {
                        Success = false,
                        ErrorMessage = "No response from YandexGPT"
                    };
                }

                var alternative = yandexResponse.Result.Alternatives[0];

                return new AIResponse
                {
                    Content = alternative.Message.Text,
                    TokensUsed = yandexResponse.Result.Usage.TotalTokens,
                    Model = yandexResponse.Result.ModelVersion,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling YandexGPT API");
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Простой запрос для проверки доступности
                var testRequest = new AIRequest
                {
                    SystemPrompt = "You are a helpful assistant.",
                    Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "Hi" }
                },
                    MaxTokens = 10
                };

                var response = await GenerateResponseAsync(testRequest, cancellationToken);
                return response.Success;
            }
            catch
            {
                return false;
            }
        }

        private YandexGPTRequest MapToYandexRequest(AIRequest request)
        {
            var messages = new List<YandexMessage>();

            // Добавляем системный промпт
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Add(new YandexMessage
                {
                    Role = "system",
                    Text = request.SystemPrompt
                });
            }

            // Добавляем историю сообщений
            foreach (var message in request.Messages)
            {
                messages.Add(new YandexMessage
                {
                    Role = message.Role,
                    Text = message.Content
                });
            }

            return new YandexGPTRequest
            {
                ModelUri = $"gpt://{_config.FolderId}/{_config.Model}",
                CompletionOptions = new CompletionOptions
                {
                    Stream = false,
                    Temperature = request.Temperature,
                    MaxTokens = request.MaxTokens
                },
                Messages = messages
            };
        }
    }
}

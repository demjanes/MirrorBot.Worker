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
            IHttpClientFactory httpClientFactory,
            IOptions<AIConfiguration> aiConfig,
            ILogger<YandexGPTProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient("yandex_gpt");
            _config = aiConfig.Value.YandexGPT;
            _logger = logger;
        }

        public async Task<AIResponse> GenerateResponseAsync(
            AIRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var modelUri = $"gpt://{_config.FolderId}/{_config.Model}";

                var messages = new List<YandexMessage>();

                // Системный промпт
                if (!string.IsNullOrEmpty(request.SystemPrompt))
                {
                    messages.Add(new YandexMessage
                    {
                        Role = "system",
                        Text = request.SystemPrompt
                    });
                }

                // Контекст
                foreach (var msg in request.Messages)
                {
                    messages.Add(new YandexMessage
                    {
                        Role = msg.Role,
                        Text = msg.Content
                    });
                }

                var yandexRequest = new YandexGPTRequest
                {
                    ModelUri = modelUri,
                    CompletionOptions = new CompletionOptions
                    {
                        Stream = false,
                        Temperature = request.Temperature,
                        MaxTokens = request.MaxTokens
                    },
                    Messages = messages
                };

                var jsonContent = JsonSerializer.Serialize(yandexRequest);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_config.BaseUrl}/completion")
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                // ✅ Используем формат Api-Key
                httpRequest.Headers.Add("Authorization", $"Api-Key {_config.ApiKey}");
                httpRequest.Headers.Add("x-folder-id", _config.FolderId);

                _logger.LogDebug(
                    "Sending request to YandexGPT: Model={Model}, Messages={Count}",
                    _config.Model,
                    messages.Count);

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "YandexGPT API error: {StatusCode} - {Content}",
                        response.StatusCode,
                        responseContent);

                    return new AIResponse
                    {
                        Success = false,
                        ErrorMessage = $"API error: {response.StatusCode} - {responseContent}"
                    };
                }

                var yandexResponse = JsonSerializer.Deserialize<YandexGPTResponse>(responseContent);

                if (yandexResponse?.Result?.Alternatives == null ||
                    yandexResponse.Result.Alternatives.Count == 0)
                {
                    _logger.LogError("No response from YandexGPT: {Content}", responseContent);

                    return new AIResponse
                    {
                        Success = false,
                        ErrorMessage = "No response from YandexGPT"
                    };
                }

                var alternative = yandexResponse.Result.Alternatives[0];
                var tokensUsed = yandexResponse.Result.Usage?.TotalTokensInt ?? 0;

                _logger.LogInformation(
                    "YandexGPT response received: Tokens={Tokens}",
                    tokensUsed);

                return new AIResponse
                {
                    Content = alternative.Message.Text,
                    TokensUsed = tokensUsed,
                    Model = _config.Model,
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
                // Проверяем доступность API простым запросом
                var testRequest = new AIRequest
                {
                    SystemPrompt = "Test",
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "user", Content = "Hi" }
                    },
                    MaxTokens = 10
                };

                var response = await GenerateResponseAsync(testRequest, cancellationToken);

                return response.Success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "YandexGPT availability check failed");
                return false;
            }
        }
    }
}

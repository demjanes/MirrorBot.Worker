using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Services.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace MirrorBot.Worker.Services.AI.Providers.YandexGPT
{
    /// <summary>
    /// Провайдер для работы с Yandex SpeechKit (STT/TTS)
    /// </summary>
    public class YandexSpeechKitProvider : ISpeechProvider
    {
        private readonly HttpClient _httpClient;
        private readonly YandexSpeechKitConfig _config;
        private readonly ILogger<YandexSpeechKitProvider> _logger;

        public string ProviderName => "YandexSpeechKit";

        public YandexSpeechKitProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<SpeechConfiguration> speechConfig,
            ILogger<YandexSpeechKitProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient("yandex_speech");
            _config = speechConfig.Value.YandexSpeechKit;
            _logger = logger;
        }

        public async Task<SpeechToTextResponse> TranscribeAudioAsync(
            SpeechToTextRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(new ByteArrayContent(request.AudioData), "audio", "audio.ogg");
                content.Add(new StringContent(_config.FolderId), "folderId");
                content.Add(new StringContent(request.Language), "lang");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, _config.SttUrl)
                {
                    Content = content
                };

                httpRequest.Headers.Add("Authorization", $"Api-Key {_config.ApiKey}");

                _logger.LogDebug("Sending STT request to YandexSpeechKit");

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "YandexSpeechKit STT error: {StatusCode} - {Content}",
                        response.StatusCode,
                        responseText);

                    return new SpeechToTextResponse
                    {
                        Success = false,
                        ErrorMessage = $"STT API error: {response.StatusCode} - {responseText}"
                    };
                }

                var result = JsonSerializer.Deserialize<YandexSttResponse>(
                    responseText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("STT response received: {Text}", result?.Result);

                return new SpeechToTextResponse
                {
                    Text = result?.Result ?? string.Empty,
                    Success = true,
                    Confidence = 0.95, // Yandex не возвращает confidence, ставим высокое
                    Pronunciation = request.AnalyzePronunciation
                        ? new PronunciationAnalysis { Score = 85 } // TODO: реальный анализ
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transcribing audio");

                return new SpeechToTextResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TextToSpeechResponse> GenerateSpeechAsync(
            TextToSpeechRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    ["text"] = request.Text,
                    ["lang"] = _config.Language,
                    ["voice"] = request.Voice ?? _config.Voice,
                    ["speed"] = request.Speed.ToString(CultureInfo.InvariantCulture),
                    ["format"] = "oggopus",
                    ["folderId"] = _config.FolderId
                };

                var content = new FormUrlEncodedContent(parameters);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, _config.TtsUrl)
                {
                    Content = content
                };

                httpRequest.Headers.Add("Authorization", $"Api-Key {_config.ApiKey}");

                _logger.LogDebug("Sending TTS request to YandexSpeechKit");

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "YandexSpeechKit TTS error: {StatusCode} - {Content}",
                        response.StatusCode,
                        errorText);

                    return new TextToSpeechResponse
                    {
                        Success = false,
                        ErrorMessage = $"TTS API error: {response.StatusCode} - {errorText}"
                    };
                }

                var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                _logger.LogInformation("TTS response received: {Size} bytes", audioData.Length);

                return new TextToSpeechResponse
                {
                    AudioData = audioData,
                    AudioFormat = "ogg_opus",
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating speech");

                return new TextToSpeechResponse
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
                // Проверяем TTS минимальным запросом
                var testRequest = new TextToSpeechRequest
                {
                    Text = "Test",
                    Voice = _config.Voice,
                    Speed = 1.0
                };

                var response = await GenerateSpeechAsync(testRequest, cancellationToken);

                return response.Success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "YandexSpeechKit availability check failed");
                return false;
            }
        }

        private class YandexSttResponse
        {
            public string? Result { get; set; }
        }
    }
}

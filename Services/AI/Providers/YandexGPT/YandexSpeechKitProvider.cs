using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Services.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
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
            HttpClient httpClient,
            IOptions<SpeechConfiguration> speechConfig,
            ILogger<YandexSpeechKitProvider> logger)
        {
            _httpClient = httpClient;
            _config = speechConfig.Value.YandexSpeechKit;
            _logger = logger;

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Api-Key", _config.ApiKey);
        }

        public async Task<SpeechToTextResponse> TranscribeAudioAsync(
            SpeechToTextRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Transcribing audio using Yandex SpeechKit");

                // Yandex SpeechKit принимает аудио в формате LINEAR16 PCM
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["topic"] = "general";
                queryParams["lang"] = request.Language;
                queryParams["format"] = "oggopus"; // Telegram voice format
                queryParams["folderId"] = _config.FolderId;

                var url = $"{_config.SttUrl}?{queryParams}";

                var content = new ByteArrayContent(request.AudioData);
                content.Headers.ContentType = new MediaTypeHeaderValue("audio/ogg");

                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("SpeechKit STT error: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    return new SpeechToTextResponse
                    {
                        Success = false,
                        ErrorMessage = $"STT Error: {response.StatusCode}"
                    };
                }

                var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);

                // Yandex возвращает JSON с полем "result"
                var result = System.Text.Json.JsonSerializer.Deserialize<
                    System.Collections.Generic.Dictionary<string, string>>(resultJson);

                if (result == null || !result.ContainsKey("result"))
                {
                    return new SpeechToTextResponse
                    {
                        Success = false,
                        ErrorMessage = "No transcription result"
                    };
                }

                return new SpeechToTextResponse
                {
                    Text = result["result"],
                    Success = true,
                    Confidence = 0.9 // Yandex не всегда возвращает confidence
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
                _logger.LogInformation("Generating speech using Yandex SpeechKit");

                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["text"] = request.Text;
                queryParams["lang"] = ExtractLanguage(request.Voice);
                queryParams["voice"] = ExtractVoiceName(request.Voice);
                queryParams["speed"] = request.Speed.ToString("F1");
                queryParams["format"] = "oggopus"; // Telegram compatible
                queryParams["folderId"] = _config.FolderId;

                var url = $"{_config.TtsUrl}?{queryParams}";

                var response = await _httpClient.PostAsync(
                    url,
                    new StringContent(string.Empty),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("SpeechKit TTS error: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    return new TextToSpeechResponse
                    {
                        Success = false,
                        ErrorMessage = $"TTS Error: {response.StatusCode}"
                    };
                }

                var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

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

        private string ExtractLanguage(string voice)
        {
            // Маппинг голосов на языки
            // jane, john, omazh - английские голоса
            // alena, filipp - русские голоса
            return voice.ToLower() switch
            {
                "jane" or "john" or "omazh" => "en-US",
                "alena" or "filipp" => "ru-RU",
                _ => "en-US"
            };
        }

        private string ExtractVoiceName(string voice)
        {
            // Если передан формат "en-US-jane", извлекаем "jane"
            var parts = voice.Split('-');
            return parts.Length > 2 ? parts[2] : voice;
        }
    }
}

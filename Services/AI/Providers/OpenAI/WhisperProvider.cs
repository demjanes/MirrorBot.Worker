using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Services.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Providers.OpenAI
{
    /// <summary>
    /// Провайдер для работы с Whisper (будущая реализация)
    /// </summary>
    public class WhisperProvider : ISpeechProvider
    {
        private readonly ILogger<WhisperProvider> _logger;
        private readonly OpenAISpeechConfig _config;

        public string ProviderName => "Whisper";

        public WhisperProvider(
            IOptions<SpeechConfiguration> speechConfig,
            ILogger<WhisperProvider> logger)
        {
            _config = speechConfig.Value.OpenAI;
            _logger = logger;
        }

        public Task<SpeechToTextResponse> TranscribeAudioAsync(
            SpeechToTextRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Whisper provider is not yet implemented");
            return Task.FromResult(new SpeechToTextResponse
            {
                Success = false,
                ErrorMessage = "Whisper provider is not yet implemented. Please use YandexSpeechKit."
            });
        }

        public Task<TextToSpeechResponse> GenerateSpeechAsync(
            TextToSpeechRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("OpenAI TTS provider is not yet implemented");
            return Task.FromResult(new TextToSpeechResponse
            {
                Success = false,
                ErrorMessage = "OpenAI TTS is not yet implemented. Please use YandexSpeechKit."
            });
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Проверка доступности через минимальный TTS запрос
                var testRequest = new TextToSpeechRequest
                {
                    Text = "Test",
                    Voice = _config.Voice
                };

                var response = await GenerateSpeechAsync(testRequest, cancellationToken);

                return response.Success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI Speech availability check failed");
                return false;
            }
        }
    }
}

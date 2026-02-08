using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Services.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI
{

    /// <summary>
    /// Фабрика для создания AI провайдеров
    /// </summary>
    public class AIProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AIConfiguration _config;

        public AIProviderFactory(
            IServiceProvider serviceProvider,
            IOptions<AIConfiguration> config)
        {
            _serviceProvider = serviceProvider;
            _config = config.Value;
        }

        public IAIProvider GetProvider()
        {
            return _config.Provider.ToLower() switch
            {
                "yandexgpt" => _serviceProvider.GetRequiredService<Providers.YandexGPT.YandexGPTProvider>(),
                "openai" => _serviceProvider.GetRequiredService<Providers.OpenAI.OpenAIProvider>(),
                _ => throw new InvalidOperationException($"Unknown AI provider: {_config.Provider}")
            };
        }
    }

    /// <summary>
    /// Фабрика для создания Speech провайдеров
    /// </summary>
    public class SpeechProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SpeechConfiguration _config;

        public SpeechProviderFactory(
            IServiceProvider serviceProvider,
            IOptions<SpeechConfiguration> config)
        {
            _serviceProvider = serviceProvider;
            _config = config.Value;
        }

        public ISpeechProvider GetProvider()
        {
            return _config.Provider.ToLower() switch
            {
                "yandexspeechkit" => _serviceProvider.GetRequiredService<Providers.YandexGPT.YandexSpeechKitProvider>(),
                "whisper" or "openai" => _serviceProvider.GetRequiredService<Providers.OpenAI.WhisperProvider>(),
                _ => throw new InvalidOperationException($"Unknown speech provider: {_config.Provider}")
            };
        }
    }
}

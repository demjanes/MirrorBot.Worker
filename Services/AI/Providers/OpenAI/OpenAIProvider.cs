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
    /// Провайдер для работы с OpenAI API (будущая реализация)
    /// </summary>
    public class OpenAIProvider : IAIProvider
    {
        private readonly ILogger<OpenAIProvider> _logger;
        private readonly OpenAIConfig _config;

        public string ProviderName => "OpenAI";

        public OpenAIProvider(
            IOptions<AIConfiguration> aiConfig,
            ILogger<OpenAIProvider> logger)
        {
            _config = aiConfig.Value.OpenAI;
            _logger = logger;
        }

        public Task<AIResponse> GenerateResponseAsync(
            AIRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("OpenAI provider is not yet implemented");
            return Task.FromResult(new AIResponse
            {
                Success = false,
                ErrorMessage = "OpenAI provider is not yet implemented. Please use YandexGPT."
            });
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Services.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class YooKassaWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly YooKassaConfiguration _config;
        private readonly ILogger<YooKassaWebhookController> _logger;

        public YooKassaWebhookController(
            IPaymentService paymentService,
            IOptions<YooKassaConfiguration> config,
            ILogger<YooKassaWebhookController> logger)
        {
            _paymentService = paymentService;
            _config = config.Value;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint для webhook от ЮКассы.
        /// </summary>
        [HttpPost("yookassa")]
        public async Task<IActionResult> YooKassaWebhook(CancellationToken cancellationToken)
        {
            try
            {
                // Читаем тело запроса
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var webhookJson = await reader.ReadToEndAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(webhookJson))
                {
                    _logger.LogWarning("Received empty webhook from YooKassa");
                    return BadRequest("Empty request body");
                }

                _logger.LogInformation("Received YooKassa webhook");
                _logger.LogDebug("Webhook payload: {Payload}", webhookJson);

                // Проверяем Basic Auth (опционально, для безопасности)
                if (!ValidateBasicAuth())
                {
                    _logger.LogWarning("YooKassa webhook: Invalid authentication");
                    return Unauthorized();
                }

                // Обрабатываем webhook
                var success = await _paymentService.ProcessWebhookAsync(webhookJson, cancellationToken);

                if (!success)
                {
                    _logger.LogWarning("Failed to process YooKassa webhook");
                    return Ok(); // Возвращаем 200, чтобы ЮКасса не повторяла запрос
                }

                _logger.LogInformation("YooKassa webhook processed successfully");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing YooKassa webhook");
                return Ok(); // Возвращаем 200, чтобы избежать повторных запросов
            }
        }

        /// <summary>
        /// Проверка Basic Auth от ЮКассы.
        /// </summary>
        private bool ValidateBasicAuth()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                return false;
            }

            var authHeaderValue = authHeader.ToString();
            if (!authHeaderValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                var encodedCredentials = authHeaderValue.Substring("Basic ".Length).Trim();
                var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var credentials = decodedCredentials.Split(':', 2);

                if (credentials.Length != 2)
                {
                    return false;
                }

                var shopId = credentials[0];
                var secretKey = credentials[1];

                // Проверяем, что credentials совпадают с конфигом
                return shopId == _config.ShopId && secretKey == _config.SecretKey;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Health check endpoint.
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}

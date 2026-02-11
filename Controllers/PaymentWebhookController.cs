using Microsoft.AspNetCore.Mvc;
using MirrorBot.Worker.Data.Models.Payments;
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
    public class PaymentWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentWebhookController> _logger;

        public PaymentWebhookController(
            IPaymentService paymentService,
            ILogger<PaymentWebhookController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Webhook для ЮКассы.
        /// </summary>
        [HttpPost("yookassa")]
        public async Task<IActionResult> YooKassaWebhook(CancellationToken cancellationToken)
        {
            return await ProcessWebhook(PaymentProvider.YooKassa, cancellationToken);
        }

        /// <summary>
        /// Webhook для Stripe (для будущего).
        /// </summary>
        [HttpPost("stripe")]
        public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
        {
            return await ProcessWebhook(PaymentProvider.Stripe, cancellationToken);
        }

        /// <summary>
        /// Health check endpoint.
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        private async Task<IActionResult> ProcessWebhook(
            PaymentProvider provider,
            CancellationToken cancellationToken)
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var webhookData = await reader.ReadToEndAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(webhookData))
                {
                    _logger.LogWarning("Received empty webhook from {Provider}", provider);
                    return BadRequest("Empty request body");
                }

                _logger.LogInformation("Received webhook from {Provider}", provider);
                _logger.LogDebug("Webhook payload: {Payload}", webhookData);

                var success = await _paymentService.ProcessWebhookAsync(
                    provider,
                    webhookData,
                    cancellationToken);

                if (!success)
                {
                    _logger.LogWarning("Failed to process webhook from {Provider}", provider);
                }

                return Ok(); // Всегда возвращаем 200
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook from {Provider}", provider);
                return Ok(); // Всегда возвращаем 200
            }
        }
    }
}

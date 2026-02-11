using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs.Payments;
using MirrorBot.Worker.Data.Models.Payments;
using MirrorBot.Worker.Services.Payments.Providers.YooKassa.Models;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MirrorBot.Worker.Services.Payments.Providers.YooKassa
{
    public class YooKassaPaymentProvider : IPaymentProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly YooKassaConfiguration _config;
        private readonly ILogger<YooKassaPaymentProvider> _logger;

        private const string ApiUrl = "https://api.yookassa.ru/v3/payments";

        public PaymentProvider ProviderType => PaymentProvider.YooKassa;

        public YooKassaPaymentProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<YooKassaConfiguration> config,
            ILogger<YooKassaPaymentProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config.Value;
            _logger = logger;
        }

        /// <summary>
        /// Создать платеж в ЮКассе.
        /// </summary>
        public async Task<CreatePaymentResult> CreatePaymentAsync(
            CreatePaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Преобразуем наш универсальный запрос в формат ЮКассы
                var yooKassaRequest = new YooKassaCreatePaymentRequest
                {
                    Amount = new AmountDto
                    {
                        Value = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
                        Currency = request.Currency
                    },
                    Confirmation = new ConfirmationDto
                    {
                        Type = "redirect",
                        ReturnUrl = request.ReturnUrl
                    },
                    Capture = true,
                    Description = request.Description,
                    Metadata = request.Metadata
                };

                var response = await CallYooKassaApiAsync(yooKassaRequest, cancellationToken);

                if (response == null)
                {
                    return new CreatePaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create payment in YooKassa"
                    };
                }

                return new CreatePaymentResult
                {
                    Success = true,
                    ExternalPaymentId = response.Id,
                    PaymentUrl = response.Confirmation?.ConfirmationUrl,
                    ProviderData = JsonSerializer.Serialize(response)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment in YooKassa");
                return new CreatePaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Обработать webhook от ЮКассы.
        /// </summary>
        public async Task<WebhookResult> ProcessWebhookAsync(
            string webhookData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var webhook = JsonSerializer.Deserialize<YooKassaWebhook>(webhookData);

                if (webhook == null)
                {
                    return new WebhookResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid webhook data"
                    };
                }

                var status = webhook.Event switch
                {
                    "payment.succeeded" => PaymentStatus.Succeeded,
                    "payment.canceled" => PaymentStatus.Canceled,
                    _ => PaymentStatus.Pending
                };

                return new WebhookResult
                {
                    Success = true,
                    ExternalPaymentId = webhook.Object.Id,
                    Status = status,
                    PaidAtUtc = webhook.Event == "payment.succeeded" ? DateTime.UtcNow : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing YooKassa webhook");
                return new WebhookResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Проверить статус платежа в ЮКассе.
        /// </summary>
        public async Task<PaymentStatus> CheckPaymentStatusAsync(
            string externalPaymentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var httpClient = CreateHttpClient();
                var response = await httpClient.GetAsync($"{ApiUrl}/{externalPaymentId}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return PaymentStatus.Failed;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var payment = JsonSerializer.Deserialize<YooKassaPaymentResponse>(json);

                return payment?.Status switch
                {
                    "succeeded" => PaymentStatus.Succeeded,
                    "canceled" => PaymentStatus.Canceled,
                    "pending" => PaymentStatus.Pending,
                    _ => PaymentStatus.Failed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status in YooKassa");
                return PaymentStatus.Failed;
            }
        }

        /// <summary>
        /// Вызов API ЮКассы для создания платежа.
        /// </summary>
        private async Task<YooKassaPaymentResponse?> CallYooKassaApiAsync(
            YooKassaCreatePaymentRequest request,
            CancellationToken cancellationToken)
        {
            var httpClient = CreateHttpClient();
            httpClient.DefaultRequestHeaders.Add("Idempotence-Key", Guid.NewGuid().ToString());

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(ApiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("YooKassa API error: {StatusCode}, {Error}", response.StatusCode, error);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<YooKassaPaymentResponse>(responseJson);
        }

        /// <summary>
        /// Создать HttpClient с авторизацией для ЮКассы.
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var authValue = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_config.ShopId}:{_config.SecretKey}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            return httpClient;
        }
    }
}

using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Models.Payments;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Services.Payments.Models;
using MirrorBot.Worker.Services.Referral;
using MirrorBot.Worker.Services.Subscr;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Payments
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly ISubscriptionPlanRepository _planRepo;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IReferralService _referralService;
        private readonly IUsersRepository _usersRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly YooKassaConfiguration _config;
        private readonly ILogger<PaymentService> _logger;

        private const string YooKassaApiUrl = "https://api.yookassa.ru/v3/payments";

        public PaymentService(
            IPaymentRepository paymentRepo,
            ISubscriptionPlanRepository planRepo,
            ISubscriptionService subscriptionService,
            IReferralService referralService,
            IUsersRepository usersRepo,
            IHttpClientFactory httpClientFactory,
            IOptions<YooKassaConfiguration> config,
            ILogger<PaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _planRepo = planRepo;
            _subscriptionService = subscriptionService;
            _referralService = referralService;
            _usersRepo = usersRepo;
            _httpClientFactory = httpClientFactory;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<(bool Success, string? PaymentUrl, string? ErrorMessage)> CreatePaymentAsync(
     long userId,
     ObjectId planId,
     CancellationToken cancellationToken = default)
        {
            try
            {
                // Получаем тарифный план
                var plan = await _planRepo.GetByIdAsync(planId, cancellationToken);
                if (plan == null)
                {
                    return (false, null, "Тарифный план не найден.");
                }

                if (!plan.IsActive)
                {
                    return (false, null, "Тарифный план неактивен.");
                }

                // Проверяем, есть ли реферер
                var user = await _usersRepo.GetByTelegramIdAsync(userId, cancellationToken);
                long? referrerId = user?.ReferrerOwnerTgUserId;

                // Вычисляем реферальное вознаграждение (25%)
                decimal? referralReward = null;
                if (referrerId.HasValue)
                {
                    referralReward = plan.PriceRub * (_config.ReferralRewardPercent / 100m);
                }

                // Создаем запись о платеже в БД
                var payment = new Payment
                {
                    UserId = userId,
                    PlanId = planId,
                    SubscriptionType = plan.Type,
                    AmountRub = plan.PriceRub,
                    Status = PaymentStatus.Pending,
                    ReferrerUserId = referrerId,
                    ReferralRewardRub = referralReward,
                    ReferralRewardProcessed = false
                };

                payment = await _paymentRepo.CreateAsync(payment, cancellationToken);

                // Создаем платеж в ЮКассе
                var yookassaRequest = new CreatePaymentRequest
                {
                    Amount = new AmountDto
                    {
                        Value = plan.PriceRub.ToString("F2"),
                        Currency = "RUB"
                    },
                    Confirmation = new ConfirmationDto
                    {
                        Type = "redirect",
                        ReturnUrl = _config.ReturnUrl
                    },
                    Capture = true,
                    Description = $"Оплата подписки: {plan.Name}",
                    Metadata = new Dictionary<string, string>
            {
                { "payment_id", payment.Id.ToString() },
                { "user_id", userId.ToString() },
                { "plan_id", planId.ToString() }
            }
                };

                var yookassaResponse = await CreateYooKassaPaymentAsync(yookassaRequest, cancellationToken);

                if (yookassaResponse == null)
                {
                    return (false, null, "Ошибка при создании платежа в ЮКассе.");
                }

                // ✅ ИСПРАВЛЕНО: Обновление через репозиторий
                await _paymentRepo.UpdateYookassaDataAsync(
                    payment.Id,
                    yookassaResponse.Id,
                    yookassaResponse.Confirmation?.ConfirmationUrl,
                    JsonSerializer.Serialize(yookassaResponse.Metadata),
                    cancellationToken);

                _logger.LogInformation(
                    "Payment created: PaymentId={PaymentId}, YooKassaId={YooKassaId}, UserId={UserId}, Amount={Amount}₽",
                    payment.Id,
                    yookassaResponse.Id,
                    userId,
                    plan.PriceRub);

                return (true, yookassaResponse.Confirmation?.ConfirmationUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for user {UserId}", userId);
                return (false, null, "Произошла ошибка при создании платежа.");
            }
        }


        public async Task<bool> ProcessWebhookAsync(
            string webhookJson,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var webhook = JsonSerializer.Deserialize<YooKassaWebhook>(webhookJson);

                if (webhook == null || webhook.Event != "payment.succeeded")
                {
                    _logger.LogInformation("Webhook ignored: Event={Event}", webhook?.Event);
                    return false;
                }

                var yookassaPaymentId = webhook.Object.Id;

                // Ищем платеж в БД
                var payment = await _paymentRepo.GetByYookassaIdAsync(yookassaPaymentId, cancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found: YooKassaId={YooKassaId}", yookassaPaymentId);
                    return false;
                }

                // Проверяем, не обработан ли уже платеж
                if (payment.Status == PaymentStatus.Succeeded)
                {
                    _logger.LogInformation("Payment already processed: PaymentId={PaymentId}", payment.Id);
                    return true;
                }

                // Обновляем статус платежа
                await _paymentRepo.UpdateStatusAsync(
                    payment.Id,
                    PaymentStatus.Succeeded,
                    DateTime.UtcNow,
                    cancellationToken);

                _logger.LogInformation(
                    "Payment succeeded: PaymentId={PaymentId}, UserId={UserId}, Amount={Amount}₽",
                    payment.Id,
                    payment.UserId,
                    payment.AmountRub);

                // Активируем подписку
                var (success, errorMessage) = await _subscriptionService.UpgradeToPremiumAsync(
                    payment.UserId,
                    payment.SubscriptionType,
                    payment.YookassaPaymentId,
                    cancellationToken);

                if (!success)
                {
                    _logger.LogError(
                        "Failed to activate subscription: PaymentId={PaymentId}, Error={Error}",
                        payment.Id,
                        errorMessage);
                    return false;
                }

                // Обрабатываем реферальное вознаграждение
                if (payment.ReferrerUserId.HasValue &&
                    payment.ReferralRewardRub.HasValue &&
                    !payment.ReferralRewardProcessed)
                {
                    await ProcessReferralRewardAsync(payment, cancellationToken);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return false;
            }
        }

        public async Task<List<Payment>> GetUserPaymentsAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _paymentRepo.GetByUserIdAsync(userId, cancellationToken);
        }

        /// <summary>
        /// Обработка реферального вознаграждения.
        /// </summary>
        private async Task ProcessReferralRewardAsync(
            Payment payment,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!payment.ReferrerUserId.HasValue || !payment.ReferralRewardRub.HasValue)
                    return;

                var referrerId = payment.ReferrerUserId.Value;
                var rewardAmount = payment.ReferralRewardRub.Value;

                // ✅ ИСПРАВЛЕНО: Используем правильный метод
                await _referralService.ProcessReferralPaymentAsync(
                    referrerId,
                    payment.UserId,
                    payment.AmountRub,
                    rewardAmount,
                    cancellationToken);

                // Отмечаем, что вознаграждение обработано
                await _paymentRepo.MarkReferralRewardProcessedAsync(payment.Id, cancellationToken);

                _logger.LogInformation(
                    "Referral reward processed: ReferrerId={ReferrerId}, Amount={Amount}₽, PaymentId={PaymentId}",
                    referrerId,
                    rewardAmount,
                    payment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing referral reward for payment {PaymentId}", payment.Id);
            }
        }

        /// <summary>
        /// Создание платежа через API ЮКассы.
        /// </summary>
        private async Task<CreatePaymentResponse?> CreateYooKassaPaymentAsync(
            CreatePaymentRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                // Basic Auth: ShopId:SecretKey
                var authValue = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{_config.ShopId}:{_config.SecretKey}"));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", authValue);

                // Idempotence-Key для безопасного повтора запросов
                httpClient.DefaultRequestHeaders.Add("Idempotence-Key", Guid.NewGuid().ToString());

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(YooKassaApiUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "YooKassa API error: StatusCode={StatusCode}, Response={Response}",
                        response.StatusCode,
                        errorContent);
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<CreatePaymentResponse>(responseJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling YooKassa API");
                return null;
            }
        }
    }
}

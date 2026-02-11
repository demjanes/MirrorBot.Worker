using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Configs.Payments;
using MirrorBot.Worker.Data.Models.Payments;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Services.Payments.Providers;
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
        private readonly PaymentProviderFactory _providerFactory;
        private readonly PaymentConfiguration _paymentConfig;
        private readonly ReferralConfiguration _referralConfig;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepo,
            ISubscriptionPlanRepository planRepo,
            ISubscriptionService subscriptionService,
            IReferralService referralService,
            IUsersRepository usersRepo,
            PaymentProviderFactory providerFactory,
            IOptions<PaymentConfiguration> paymentConfig,
            IOptions<ReferralConfiguration> referralConfig,
            ILogger<PaymentService> logger)
        {
            _paymentRepo = paymentRepo;
            _planRepo = planRepo;
            _subscriptionService = subscriptionService;
            _referralService = referralService;
            _usersRepo = usersRepo;
            _providerFactory = providerFactory;
            _paymentConfig = paymentConfig.Value;
            _referralConfig = referralConfig.Value;
            _logger = logger;
        }

        public async Task<(bool Success, string? PaymentUrl, string? ErrorMessage)> CreatePaymentAsync(
            long userId,
            ObjectId planId,
            PaymentProvider? provider = null,
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

                // Получаем провайдер
                var paymentProvider = _providerFactory.GetProvider(provider);

                // Проверяем, есть ли реферер
                var user = await _usersRepo.GetByTelegramIdAsync(userId, cancellationToken);
                long? referrerId = user?.ReferrerOwnerTgUserId;

                // Вычисляем реферальное вознаграждение
                decimal? referralReward = null;
                if (referrerId.HasValue)
                {
                    referralReward = plan.PriceRub * _referralConfig.ReferralPercentage;
                }

                // Создаем запись о платеже в БД
                var payment = new Payment
                {
                    UserId = userId,
                    PlanId = planId,
                    SubscriptionType = plan.Type,
                    Amount = plan.PriceRub,
                    Currency = "RUB",
                    Provider = paymentProvider.ProviderType,
                    Status = PaymentStatus.Pending,
                    ReferrerUserId = referrerId,
                    ReferralRewardAmount = referralReward,
                    ReferralRewardProcessed = false,
                    Metadata = new Dictionary<string, string>
                    {
                        { "plan_name", plan.Name },
                        { "plan_duration_days", plan.DurationDays.ToString() }
                    }
                };

                payment = await _paymentRepo.CreateAsync(payment, cancellationToken);

                // Создаем платеж через провайдер
                var providerRequest = new CreatePaymentRequest
                {
                    Amount = plan.PriceRub,
                    Currency = "RUB",
                    Description = $"Оплата подписки: {plan.Name}",
                    ReturnUrl = "https://t.me/your_bot", // TODO: Из конфига
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_id", payment.Id.ToString() },
                        { "user_id", userId.ToString() },
                        { "plan_id", planId.ToString() }
                    }
                };

                var providerResult = await paymentProvider.CreatePaymentAsync(providerRequest, cancellationToken);

                if (!providerResult.Success)
                {
                    return (false, null, providerResult.ErrorMessage ?? "Ошибка при создании платежа.");
                }

                // ✅ ОБНОВЛЕНО: Универсальный метод вместо UpdateYookassaDataAsync
                await _paymentRepo.UpdateExternalDataAsync(
                    payment.Id,
                    providerResult.ExternalPaymentId!,
                    providerResult.PaymentUrl,
                    providerResult.ProviderData,
                    cancellationToken);

                _logger.LogInformation(
                    "Payment created: PaymentId={PaymentId}, Provider={Provider}, ExternalId={ExternalId}, UserId={UserId}, Amount={Amount}₽",
                    payment.Id,
                    paymentProvider.ProviderType,
                    providerResult.ExternalPaymentId,
                    userId,
                    plan.PriceRub);

                return (true, providerResult.PaymentUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for user {UserId}", userId);
                return (false, null, "Произошла ошибка при создании платежа.");
            }
        }

        public async Task<bool> ProcessWebhookAsync(
            PaymentProvider provider,
            string webhookData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Получаем провайдер
                var paymentProvider = _providerFactory.GetProvider(provider);

                // Обрабатываем webhook через провайдер
                var webhookResult = await paymentProvider.ProcessWebhookAsync(webhookData, cancellationToken);

                if (!webhookResult.Success || string.IsNullOrEmpty(webhookResult.ExternalPaymentId))
                {
                    _logger.LogWarning("Webhook processing failed: {Error}", webhookResult.ErrorMessage);
                    return false;
                }

                // Ищем платеж в БД
                var payment = await _paymentRepo.GetByExternalIdAsync(
                    webhookResult.ExternalPaymentId,
                    cancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning(
                        "Payment not found: Provider={Provider}, ExternalId={ExternalId}",
                        provider,
                        webhookResult.ExternalPaymentId);
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
                    webhookResult.Status,
                    webhookResult.PaidAtUtc,
                    cancellationToken);

                // Если платеж успешен - активируем подписку и обрабатываем реферал
                if (webhookResult.Status == PaymentStatus.Succeeded)
                {
                    await ProcessSuccessfulPaymentAsync(payment, cancellationToken);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook from {Provider}", provider);
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
        /// Обработка успешного платежа.
        /// </summary>
        private async Task ProcessSuccessfulPaymentAsync(
            Payment payment,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Processing successful payment: PaymentId={PaymentId}, UserId={UserId}, Amount={Amount}₽",
                payment.Id,
                payment.UserId,
                payment.Amount);

            // Активируем подписку
            var (success, errorMessage) = await _subscriptionService.UpgradeToPremiumAsync(
                payment.UserId,
                payment.SubscriptionType,
                payment.ExternalPaymentId,
                cancellationToken);

            if (!success)
            {
                _logger.LogError(
                    "Failed to activate subscription: PaymentId={PaymentId}, Error={Error}",
                    payment.Id,
                    errorMessage);
                return;
            }

            // Обрабатываем реферальное вознаграждение
            if (payment.ReferrerUserId.HasValue &&
                payment.ReferralRewardAmount.HasValue &&
                !payment.ReferralRewardProcessed)
            {
                await ProcessReferralRewardAsync(payment, cancellationToken);
            }
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
                if (!payment.ReferrerUserId.HasValue || !payment.ReferralRewardAmount.HasValue)
                    return;

                var referrerId = payment.ReferrerUserId.Value;
                var rewardAmount = payment.ReferralRewardAmount.Value;

                // Начисляем вознаграждение через ReferralService
                await _referralService.ProcessReferralPaymentAsync(
                    referrerId,
                    payment.UserId,
                    payment.Amount,
                    rewardAmount,
                    payment.ExternalPaymentId,
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
    }
}

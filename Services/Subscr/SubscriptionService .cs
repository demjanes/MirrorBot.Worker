using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Models.Subscription;
using MirrorBot.Worker.Data.Repositories.Interfaces;

namespace MirrorBot.Worker.Services.Subscr
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepo;
        private readonly ISubscriptionPlanRepository _planRepo;
        private readonly IUsageStatsRepository _usageStatsRepo;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepo,
            ISubscriptionPlanRepository planRepo,
            IUsageStatsRepository usageStatsRepo,
            ILogger<SubscriptionService> logger)
        {
            _subscriptionRepo = subscriptionRepo;
            _planRepo = planRepo;
            _usageStatsRepo = usageStatsRepo;
            _logger = logger;
        }

        public async Task<Subscription> GetOrCreateSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _subscriptionRepo.GetActiveSubscriptionAsync(
                userId,
                cancellationToken);

            if (subscription != null)
                return subscription;

            // Создать Free подписку
            _logger.LogInformation("Creating Free subscription for user {UserId}", userId);
            return await _subscriptionRepo.CreateFreeSubscriptionAsync(userId, cancellationToken);
        }

        public async Task<(bool CanSend, string? ErrorMessage)> CanSendMessageAsync(
            long userId,
            bool isVoice = false,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);

            // Проверяем истечение Premium подписки
            if (subscription.Type != SubscriptionType.Free &&
                subscription.EndDateUtc.HasValue &&
                subscription.EndDateUtc.Value < DateTime.UtcNow)
            {
                return (false, "❌ Ваша Premium подписка истекла. Продлите подписку для продолжения.");
            }

            // Premium - безлимит
            if (subscription.Type != SubscriptionType.Free)
                return (true, null);

            // Free - голосовые не доступны
            if (isVoice)
            {
                return (false, "🎤 Голосовые сообщения доступны только в Premium подписке.\n\n" +
                              "Используйте /subscription для перехода на Premium.");
            }

            // Free - проверяем лимит текстовых сообщений
            var canSend = await _subscriptionRepo.CanSendMessageAsync(
                userId,
                isVoice: false,
                cancellationToken);

            if (!canSend)
            {
                var (textUsed, _) = await _subscriptionRepo.GetUsedMessagesCountAsync(
                    userId,
                    cancellationToken);

                return (false, $"⚠️ Достигнут дневной лимит сообщений ({subscription.MessagesLimit}).\n\n" +
                              $"Использовано сегодня: {textUsed}/{subscription.MessagesLimit}\n" +
                              $"Лимит обновится: {subscription.ResetDateUtc:HH:mm UTC}\n\n" +
                              "Перейдите на Premium для безлимитного доступа: /subscription");
            }

            return (true, null);
        }

        public async Task UseMessageAsync(
            long userId,
            bool isVoice = false,
            int tokensUsed = 0,
            CancellationToken cancellationToken = default)
        {
            // Уменьшаем лимит в подписке (для Free)
            await _subscriptionRepo.UseMessageAsync(userId, isVoice, cancellationToken);

            // Записываем статистику использования
            if (isVoice)
            {
                await _usageStatsRepo.IncrementVoiceMessagesAsync(userId, cancellationToken);
            }
            else
            {
                await _usageStatsRepo.IncrementTextMessagesAsync(userId, cancellationToken);
            }

            if (tokensUsed > 0)
            {
                await _usageStatsRepo.AddTokensUsedAsync(userId, tokensUsed, cancellationToken);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> UpgradeToPremiumAsync(
            long userId,
            SubscriptionType premiumType,
            string paymentId,
            CancellationToken cancellationToken = default)
        {
            if (premiumType == SubscriptionType.Free)
            {
                return (false, "Невозможно перейти на Free тариф через upgrade.");
            }

            // Получаем тарифный план
            var plan = await _planRepo.GetByTypeAsync(premiumType, cancellationToken);

            if (plan == null)
            {
                _logger.LogError("Plan not found for type {Type}", premiumType);
                return (false, "Тарифный план не найден.");
            }

            // Обновляем подписку
            var success = await _subscriptionRepo.UpgradeSubscriptionAsync(
                userId,
                premiumType,
                plan.Id,
                paymentId,
                cancellationToken);

            if (!success)
            {
                return (false, "Ошибка при обновлении подписки.");
            }

            _logger.LogInformation(
                "User {UserId} upgraded to {Type} with payment {PaymentId}",
                userId,
                premiumType,
                paymentId);

            return (true, null);
        }

        public async Task<SubscriptionInfo> GetSubscriptionInfoAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetOrCreateSubscriptionAsync(userId, cancellationToken);
            var plan = await GetPlanForSubscriptionAsync(subscription, cancellationToken);

            var info = new SubscriptionInfo
            {
                Type = subscription.Type,
                TypeName = GetTypeName(subscription.Type),
                ExpiresAt = subscription.EndDateUtc
            };

            if (subscription.Type != SubscriptionType.Free && subscription.EndDateUtc.HasValue)
            {
                info.DaysRemaining = Math.Max(0, (subscription.EndDateUtc.Value - DateTime.UtcNow).Days);
            }

            if (plan != null)
            {
                info.DailyTextLimit = plan.DailyTextMessageLimit;
                info.DailyVoiceLimit = plan.DailyVoiceMessageLimit;
                info.VoiceResponseEnabled = plan.VoiceResponseEnabled;
                info.GrammarCorrectionEnabled = plan.GrammarCorrectionEnabled;
                info.VocabularyTrackingEnabled = plan.VocabularyTrackingEnabled;
            }
            else
            {
                // Fallback для Free
                info.DailyTextLimit = subscription.MessagesLimit;
                info.DailyVoiceLimit = 0;
                info.VoiceResponseEnabled = false;
                info.GrammarCorrectionEnabled = false;
                info.VocabularyTrackingEnabled = false;
            }

            // Получаем использование за сегодня
            var (textUsed, voiceUsed) = await _subscriptionRepo.GetUsedMessagesCountAsync(
                userId,
                cancellationToken);

            info.TextMessagesUsedToday = textUsed;
            info.VoiceMessagesUsedToday = voiceUsed;

            return info;
        }

        public async Task<List<SubscriptionPlan>> GetAvailablePremiumPlansAsync(
            CancellationToken cancellationToken = default)
        {
            return await _planRepo.GetPremiumPlansAsync(cancellationToken);
        }

        public async Task<SubscriptionPlan?> GetPlanForSubscriptionAsync(
            Subscription subscription,
            CancellationToken cancellationToken = default)
        {
            if (subscription.PlanId.HasValue)
            {
                return await _planRepo.GetByIdAsync(subscription.PlanId.Value, cancellationToken);
            }

            // Fallback - найти по типу
            return await _planRepo.GetByTypeAsync(subscription.Type, cancellationToken);
        }

        private static string GetTypeName(SubscriptionType type)
        {
            return type switch
            {
                SubscriptionType.Free => "Free",
                SubscriptionType.PremiumMonthly => "Premium - 1 месяц",
                SubscriptionType.PremiumQuarterly => "Premium - 3 месяца",
                SubscriptionType.PremiumHalfYear => "Premium - 6 месяцев",
                SubscriptionType.PremiumYearly => "Premium - 12 месяцев",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Отменить текущую подписку (деактивировать).
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> CancelSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _subscriptionRepo.GetActiveSubscriptionAsync(
                userId,
                cancellationToken);

            if (subscription == null)
            {
                return (false, "Активная подписка не найдена.");
            }

            // Нельзя "отменить" Free подписку
            if (subscription.Type == SubscriptionType.Free)
            {
                return (false, "Free подписка не может быть отменена.");
            }

            // Деактивируем подписку
            var success = await _subscriptionRepo.DeactivateSubscriptionAsync(
                userId,
                cancellationToken);

            if (!success)
            {
                return (false, "Ошибка при отмене подписки.");
            }

            _logger.LogInformation(
                "User {UserId} canceled subscription {SubscriptionId} of type {Type}",
                userId,
                subscription.Id,
                subscription.Type);

            // Создаем новую Free подписку
            await _subscriptionRepo.CreateFreeSubscriptionAsync(userId, cancellationToken);

            return (true, null);
        }
    }
}

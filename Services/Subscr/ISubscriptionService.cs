using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Models.Subscription;

namespace MirrorBot.Worker.Services.Subscr
{
    /// <summary>
    /// Сервис для работы с подписками и проверки лимитов.
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Получить или создать подписку пользователя (Free по умолчанию).
        /// </summary>
        Task<Subscription> GetOrCreateSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверить, может ли пользователь отправить сообщение.
        /// </summary>
        Task<(bool CanSend, string? ErrorMessage)> CanSendMessageAsync(
            long userId,
            bool isVoice = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Использовать сообщение (уменьшить лимит или записать статистику).
        /// </summary>
        Task UseMessageAsync(
            long userId,
            bool isVoice = false,
            int tokensUsed = 0,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить подписку на Premium.
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> UpgradeToPremiumAsync(
            long userId,
            SubscriptionType premiumType,
            string paymentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Отменить текущую подписку (деактивировать).
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> CancelSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить информацию о текущей подписке.
        /// </summary>
        Task<SubscriptionInfo> GetSubscriptionInfoAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить доступные Premium планы.
        /// </summary>
        Task<List<SubscriptionPlan>> GetAvailablePremiumPlansAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить параметры тарифного плана для подписки.
        /// </summary>
        Task<SubscriptionPlan?> GetPlanForSubscriptionAsync(
            Subscription subscription,
            CancellationToken cancellationToken = default);
    }

}

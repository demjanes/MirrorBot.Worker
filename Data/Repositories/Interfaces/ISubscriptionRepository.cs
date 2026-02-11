using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Models.Subscription;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с подписками
    /// </summary>
    public interface ISubscriptionRepository : IBaseRepository<Subscription>
    {
        /// <summary>
        /// Получить активную подписку пользователя
        /// </summary>
        Task<Subscription?> GetActiveSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создать подписку Free tier
        /// </summary>
        Task<Subscription> CreateFreeSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверить, может ли пользователь отправить сообщение (с учетом типа)
        /// </summary>
        Task<bool> CanSendMessageAsync(
            long userId,
            bool isVoice = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Использовать одно сообщение (уменьшить лимит)
        /// </summary>
        Task<bool> UseMessageAsync(
            long userId,
            bool isVoice = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить подписку на Premium
        /// </summary>
        Task<bool> UpgradeSubscriptionAsync(
            long userId,
            SubscriptionType type,
            ObjectId planId,
            string paymentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Деактивировать подписку
        /// </summary>
        Task<bool> DeactivateSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сбросить лимит сообщений (для Free tier)
        /// </summary>
        Task<bool> ResetMessagesLimitAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить количество использованных сообщений
        /// </summary>
        Task<(int TextMessages, int VoiceMessages)> GetUsedMessagesCountAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверить истекшие подписки и обновить их статус
        /// </summary>
        Task<int> ExpireSubscriptionsAsync(
            CancellationToken cancellationToken = default);
    }
}

using MongoDB.Bson;

namespace MirrorBot.Worker.Services.Referral
{
    /// <summary>
    /// Сервис уведомлений владельцев ботов о реферальной активности.
    /// </summary>
    public interface IReferralNotificationService
    {
        /// <summary>
        /// Уведомить о присоединении нового реферала.
        /// </summary>
        Task NotifyNewReferralAsync(
            long ownerTgUserId,
            long referralTgUserId,
            ObjectId? mirrorBotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Уведомить о начислении реферального бонуса.
        /// </summary>
        Task NotifyReferralEarningAsync(
            long ownerTgUserId,
            long referralTgUserId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Уведомить о запросе на вывод средств.
        /// </summary>
        Task NotifyPayoutRequestAsync(
            long ownerTgUserId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken = default);
    }
}

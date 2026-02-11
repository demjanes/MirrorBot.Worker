using MirrorBot.Worker.Data.Models.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Referral
{
    /// <summary>
    /// Сервис реферальной программы.
    /// </summary>
    public interface IReferralService
    {
        /// <summary>
        /// Зарегистрировать нового пользователя по реферальной ссылке/зеркалу.
        /// </summary>
        Task RegisterReferralAsync(
            long userId,
            long? referrerOwnerTgUserId,
            ObjectId? referrerMirrorBotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обработать успешную оплату (начислить бонус рефереру).
        /// </summary>
        Task OnPaymentSucceededAsync(
            long payerTgUserId,
            decimal amount,
            string currency,
            string paymentId,
            string source,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить статистику владельца.
        /// </summary>
        Task<ReferralStats?> GetOwnerStatsAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить историю транзакций владельца.
        /// </summary>
        Task<List<ReferralTransaction>> GetOwnerTransactionsAsync(
            long ownerTgUserId,
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Запросить вывод средств.
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> RequestPayoutAsync(
            long ownerTgUserId,
            decimal amount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обработать реферальный платеж (начислить вознаграждение и создать транзакцию).
        /// </summary>
        /// <param name="referrerId">Telegram ID реферера</param>
        /// <param name="referralUserId">Telegram ID реферала</param>
        /// <param name="paymentAmount">Сумма платежа</param>
        /// <param name="rewardAmount">Сумма вознаграждения</param>
        /// <param name="paymentId">ID платежа во внешней системе</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task ProcessReferralPaymentAsync(
            long referrerId,
            long referralUserId,
            decimal paymentAmount,
            decimal rewardAmount,
            string? paymentId = null,
            CancellationToken cancellationToken = default);
    }
}

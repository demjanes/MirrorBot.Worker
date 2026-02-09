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
        Task<System.Collections.Generic.List<ReferralTransaction>> GetOwnerTransactionsAsync(
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
    }
}

using MirrorBot.Worker.Data.Models.Payments;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    public interface IPaymentRepository : IBaseRepository<Payment>
    {
        /// <summary>
        /// Получить платеж по внешнему ID (любого провайдера).
        /// </summary>
        Task<Payment?> GetByExternalIdAsync(
            string externalPaymentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить все платежи пользователя.
        /// </summary>
        Task<List<Payment>> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить данные провайдера после создания платежа.
        /// </summary>
        Task<Payment?> UpdateExternalDataAsync(
            ObjectId paymentId,
            string externalPaymentId,
            string? paymentUrl,
            string? providerData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить статус платежа.
        /// </summary>
        Task<Payment?> UpdateStatusAsync(
            ObjectId paymentId,
            PaymentStatus status,
            DateTime? paidAtUtc = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Отметить, что реферальное вознаграждение обработано.
        /// </summary>
        Task MarkReferralRewardProcessedAsync(
            ObjectId paymentId,
            CancellationToken cancellationToken = default);
    }
}

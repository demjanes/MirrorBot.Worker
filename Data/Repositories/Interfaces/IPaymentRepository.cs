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
        /// Получить платеж по ID ЮКассы.
        /// </summary>
        Task<Payment?> GetByYookassaIdAsync(
            string yookassaPaymentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить все платежи пользователя.
        /// </summary>
        Task<List<Payment>> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить YooKassa данные после создания платежа.
        /// </summary>
        Task<Payment?> UpdateYookassaDataAsync(
            ObjectId paymentId,
            string yookassaPaymentId,
            string? confirmationUrl,
            string? metadata,
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

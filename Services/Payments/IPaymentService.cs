using MirrorBot.Worker.Data.Models.Payments;
using MongoDB.Bson;

namespace MirrorBot.Worker.Services.Payments
{
    public interface IPaymentService
    {
        /// <summary>
        /// Создать платеж для покупки подписки.
        /// </summary>
        Task<(bool Success, string? PaymentUrl, string? ErrorMessage)> CreatePaymentAsync(
            long userId,
            ObjectId planId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обработать webhook от ЮКассы.
        /// </summary>
        Task<bool> ProcessWebhookAsync(
            string webhookJson,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить платежи пользователя.
        /// </summary>
        Task<List<Payment>> GetUserPaymentsAsync(
            long userId,
            CancellationToken cancellationToken = default);
    }
}

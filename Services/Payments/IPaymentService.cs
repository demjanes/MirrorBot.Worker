using MirrorBot.Worker.Data.Models.Payments;
using MongoDB.Bson;

namespace MirrorBot.Worker.Services.Payments
{
    public interface IPaymentService
    {
        /// <summary>
        /// Создать платеж для покупки подписки.
        /// </summary>
        /// <param name="userId">Telegram ID пользователя</param>
        /// <param name="planId">ID тарифного плана</param>
        /// <param name="botUsername">Username бота, через который идет оплата</param>
        /// <param name="provider">Провайдер платежа (по умолчанию из конфига)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task<(bool Success, string? PaymentUrl, string? ErrorMessage)> CreatePaymentAsync(
            long userId,
            ObjectId planId,
            string botUsername,  // ✅ Добавлен параметр
            PaymentProvider? provider = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обработать webhook от платежного провайдера.
        /// </summary>
        Task<bool> ProcessWebhookAsync(
            PaymentProvider provider,
            string webhookData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить платежи пользователя.
        /// </summary>
        Task<List<Payment>> GetUserPaymentsAsync(
            long userId,
            CancellationToken cancellationToken = default);
    }
}

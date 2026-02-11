using MirrorBot.Worker.Data.Models.Payments;

namespace MirrorBot.Worker.Services.Payments.Providers
{
    /// <summary>
    /// Интерфейс провайдера платежей.
    /// </summary>
    public interface IPaymentProvider
    {
        /// <summary>
        /// Тип провайдера.
        /// </summary>
        PaymentProvider ProviderType { get; }

        /// <summary>
        /// Создать платеж во внешней системе.
        /// </summary>
        /// <param name="request">Данные для создания платежа</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат создания платежа</returns>
        Task<CreatePaymentResult> CreatePaymentAsync(
            CreatePaymentRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обработать webhook от провайдера.
        /// </summary>
        /// <param name="webhookData">Данные webhook</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат обработки webhook</returns>
        Task<WebhookResult> ProcessWebhookAsync(
            string webhookData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверить статус платежа во внешней системе.
        /// </summary>
        /// <param name="externalPaymentId">ID платежа во внешней системе</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Статус платежа</returns>
        Task<PaymentStatus> CheckPaymentStatusAsync(
            string externalPaymentId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Запрос на создание платежа.
    /// </summary>
    public class CreatePaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// Результат создания платежа.
    /// </summary>
    public class CreatePaymentResult
    {
        public bool Success { get; set; }
        public string? ExternalPaymentId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ProviderData { get; set; }
    }

    /// <summary>
    /// Результат обработки webhook.
    /// </summary>
    public class WebhookResult
    {
        public bool Success { get; set; }
        public string? ExternalPaymentId { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

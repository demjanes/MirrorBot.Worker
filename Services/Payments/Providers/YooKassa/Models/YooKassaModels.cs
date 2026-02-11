using System.Text.Json.Serialization;

namespace MirrorBot.Worker.Services.Payments.Providers.YooKassa.Models
{
    /// <summary>
    /// Запрос на создание платежа в ЮКассе.
    /// </summary>
    public class YooKassaCreatePaymentRequest
    {
        /// <summary>
        /// Сумма платежа.
        /// </summary>
        [JsonPropertyName("amount")]
        public AmountDto Amount { get; set; } = new();

        /// <summary>
        /// Способ подтверждения платежа.
        /// </summary>
        [JsonPropertyName("confirmation")]
        public ConfirmationDto Confirmation { get; set; } = new();

        /// <summary>
        /// Автоматический прием поступившего платежа.
        /// </summary>
        [JsonPropertyName("capture")]
        public bool Capture { get; set; } = true;

        /// <summary>
        /// Описание транзакции.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Метаданные платежа.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// Ответ от ЮКассы при создании платежа.
    /// </summary>
    public class YooKassaPaymentResponse
    {
        /// <summary>
        /// ID платежа в ЮКассе.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Статус платежа.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Сумма платежа.
        /// </summary>
        [JsonPropertyName("amount")]
        public AmountDto Amount { get; set; } = new();

        /// <summary>
        /// Описание транзакции.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Данные для подтверждения платежа.
        /// </summary>
        [JsonPropertyName("confirmation")]
        public ConfirmationDto? Confirmation { get; set; }

        /// <summary>
        /// Дата и время создания платежа.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Признак оплаты заказа.
        /// </summary>
        [JsonPropertyName("paid")]
        public bool Paid { get; set; }

        /// <summary>
        /// Метаданные платежа.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Признак тестовой операции.
        /// </summary>
        [JsonPropertyName("test")]
        public bool Test { get; set; }

        /// <summary>
        /// Способ проведения платежа.
        /// </summary>
        [JsonPropertyName("payment_method")]
        public PaymentMethodDto? PaymentMethod { get; set; }
    }

    /// <summary>
    /// Webhook уведомление от ЮКассы.
    /// </summary>
    public class YooKassaWebhook
    {
        /// <summary>
        /// Тип уведомления (всегда "notification").
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Событие, которое произошло.
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        /// <summary>
        /// Объект с данными платежа.
        /// </summary>
        [JsonPropertyName("object")]
        public YooKassaPaymentResponse Object { get; set; } = new();
    }

    /// <summary>
    /// Сумма платежа.
    /// </summary>
    public class AmountDto
    {
        /// <summary>
        /// Сумма в виде строки (например "499.00").
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Код валюты (ISO 4217).
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "RUB";
    }

    /// <summary>
    /// Данные для подтверждения платежа.
    /// </summary>
    public class ConfirmationDto
    {
        /// <summary>
        /// Тип подтверждения (redirect, embedded и т.д.).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "redirect";

        /// <summary>
        /// URL для возврата после оплаты.
        /// </summary>
        [JsonPropertyName("return_url")]
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// URL для перехода на страницу оплаты (возвращается API).
        /// </summary>
        [JsonPropertyName("confirmation_url")]
        public string? ConfirmationUrl { get; set; }

        /// <summary>
        /// Токен для встраиваемого виджета (для embedded).
        /// </summary>
        [JsonPropertyName("confirmation_token")]
        public string? ConfirmationToken { get; set; }
    }

    /// <summary>
    /// Способ оплаты.
    /// </summary>
    public class PaymentMethodDto
    {
        /// <summary>
        /// Тип платежного средства.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// ID платежного средства.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Признак сохраненного платежного средства.
        /// </summary>
        [JsonPropertyName("saved")]
        public bool Saved { get; set; }

        /// <summary>
        /// Название платежного средства.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Данные банковской карты (если тип = bank_card).
        /// </summary>
        [JsonPropertyName("card")]
        public CardDto? Card { get; set; }
    }

    /// <summary>
    /// Данные банковской карты.
    /// </summary>
    public class CardDto
    {
        /// <summary>
        /// Первые 6 цифр номера карты (BIN).
        /// </summary>
        [JsonPropertyName("first6")]
        public string? First6 { get; set; }

        /// <summary>
        /// Последние 4 цифры номера карты.
        /// </summary>
        [JsonPropertyName("last4")]
        public string? Last4 { get; set; }

        /// <summary>
        /// Срок действия (формат MM/YYYY).
        /// </summary>
        [JsonPropertyName("expiry_month")]
        public string? ExpiryMonth { get; set; }

        /// <summary>
        /// Год срока действия.
        /// </summary>
        [JsonPropertyName("expiry_year")]
        public string? ExpiryYear { get; set; }

        /// <summary>
        /// Тип карты (MasterCard, Visa и т.д.).
        /// </summary>
        [JsonPropertyName("card_type")]
        public string? CardType { get; set; }

        /// <summary>
        /// Страна-эмитент карты (ISO 3166).
        /// </summary>
        [JsonPropertyName("issuer_country")]
        public string? IssuerCountry { get; set; }

        /// <summary>
        /// Название банка-эмитента.
        /// </summary>
        [JsonPropertyName("issuer_name")]
        public string? IssuerName { get; set; }
    }

    /// <summary>
    /// Ошибка от API ЮКассы.
    /// </summary>
    public class YooKassaErrorResponse
    {
        /// <summary>
        /// Тип ошибки.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// ID ошибки.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Код ошибки.
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Описание ошибки.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Название параметра, в котором произошла ошибка.
        /// </summary>
        [JsonPropertyName("parameter")]
        public string? Parameter { get; set; }
    }

    /// <summary>
    /// Возврат платежа (refund).
    /// </summary>
    public class YooKassaRefundRequest
    {
        /// <summary>
        /// ID платежа для возврата.
        /// </summary>
        [JsonPropertyName("payment_id")]
        public string PaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Сумма возврата.
        /// </summary>
        [JsonPropertyName("amount")]
        public AmountDto Amount { get; set; } = new();

        /// <summary>
        /// Комментарий к операции.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Ответ на запрос возврата.
    /// </summary>
    public class YooKassaRefundResponse
    {
        /// <summary>
        /// ID возврата.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// ID платежа.
        /// </summary>
        [JsonPropertyName("payment_id")]
        public string PaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Статус возврата.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Сумма возврата.
        /// </summary>
        [JsonPropertyName("amount")]
        public AmountDto Amount { get; set; } = new();

        /// <summary>
        /// Дата создания возврата.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Комментарий к операции.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}

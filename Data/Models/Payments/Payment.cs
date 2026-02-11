using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Models.Subscription;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.Payments
{
    /// <summary>
    /// Универсальный платеж пользователя.
    /// </summary>
    public sealed class Payment : BaseEntity
    {
        /// <summary>
        /// Telegram ID пользователя, который совершил платеж.
        /// </summary>
        [BsonElement("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// ID тарифного плана.
        /// </summary>
        [BsonElement("planId")]
        public ObjectId PlanId { get; set; }

        /// <summary>
        /// Тип подписки.
        /// </summary>
        [BsonElement("subscriptionType")]
        [BsonRepresentation(BsonType.String)]
        public SubscriptionType SubscriptionType { get; set; }

        /// <summary>
        /// Сумма платежа.
        /// </summary>
        [BsonElement("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Валюта платежа.
        /// </summary>
        [BsonElement("currency")]
        public string Currency { get; set; } = "RUB";

        /// <summary>
        /// Провайдер платежа (YooKassa, Stripe, Crypto и т.д.).
        /// </summary>
        [BsonElement("provider")]
        [BsonRepresentation(BsonType.String)]
        public PaymentProvider Provider { get; set; }

        /// <summary>
        /// ID платежа во внешней системе (уникальный для каждого провайдера).
        /// </summary>
        [BsonElement("externalPaymentId")]
        public string ExternalPaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Статус платежа.
        /// </summary>
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// URL для оплаты (если есть).
        /// </summary>
        [BsonElement("paymentUrl")]
        public string? PaymentUrl { get; set; }

        /// <summary>
        /// Дата последнего обновления статуса.
        /// </summary>
        [BsonElement("updatedAtUtc")]
        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>
        /// Дата успешной оплаты.
        /// </summary>
        [BsonElement("paidAtUtc")]
        public DateTime? PaidAtUtc { get; set; }

        /// <summary>
        /// ID реферера (если есть).
        /// </summary>
        [BsonElement("referrerUserId")]
        public long? ReferrerUserId { get; set; }

        /// <summary>
        /// Сумма реферального вознаграждения.
        /// </summary>
        [BsonElement("referralRewardAmount")]
        public decimal? ReferralRewardAmount { get; set; }

        /// <summary>
        /// Было ли начислено реферальное вознаграждение.
        /// </summary>
        [BsonElement("referralRewardProcessed")]
        public bool ReferralRewardProcessed { get; set; }

        /// <summary>
        /// Дополнительные данные от провайдера (JSON).
        /// Хранит специфичную для провайдера информацию.
        /// </summary>
        [BsonElement("providerData")]
        public string? ProviderData { get; set; }

        /// <summary>
        /// Метаданные платежа (для внутреннего использования).
        /// </summary>
        [BsonElement("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}

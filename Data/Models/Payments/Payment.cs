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
    /// Платеж пользователя.
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
        /// Сумма платежа в рублях.
        /// </summary>
        [BsonElement("amountRub")]
        public decimal AmountRub { get; set; }

        /// <summary>
        /// ID платежа в ЮКассе.
        /// </summary>
        [BsonElement("yookassaPaymentId")]
        public string YookassaPaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Статус платежа.
        /// </summary>
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// URL для оплаты (confirmation URL от ЮКассы).
        /// </summary>
        [BsonElement("confirmationUrl")]
        public string? ConfirmationUrl { get; set; }

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
        /// Сумма реферального вознаграждения (25% от AmountRub).
        /// </summary>
        [BsonElement("referralRewardRub")]
        public decimal? ReferralRewardRub { get; set; }

        /// <summary>
        /// Было ли начислено реферальное вознаграждение.
        /// </summary>
        [BsonElement("referralRewardProcessed")]
        public bool ReferralRewardProcessed { get; set; }

        /// <summary>
        /// Метаданные от ЮКассы (JSON).
        /// </summary>
        [BsonElement("metadata")]
        public string? Metadata { get; set; }
    }
}

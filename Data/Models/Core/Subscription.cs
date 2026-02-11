using MirrorBot.Worker.Data.Models.Subscription;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Подписка пользователя на сервис английского тьютора
    /// </summary>
    public sealed class Subscription : BaseEntity
    {
        /// <summary>
        /// ID пользователя Telegram
        /// </summary>
        [BsonElement("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// Тип подписки
        /// </summary>
        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public SubscriptionType Type { get; set; } = SubscriptionType.Free;

        /// <summary>
        /// Дата начала подписки
        /// </summary>
        [BsonElement("startDateUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartDateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Дата окончания подписки
        /// </summary>
        [BsonElement("endDateUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? EndDateUtc { get; set; }

        /// <summary>
        /// Лимит сообщений (для Free тарифа)
        /// </summary>
        [BsonElement("messagesLimit")]
        public int MessagesLimit { get; set; } = 10;

        /// <summary>
        /// Использовано сообщений в текущем периоде
        /// </summary>
        [BsonElement("messagesUsed")]
        public int MessagesUsed { get; set; } = 0;

        /// <summary>
        /// Дата сброса счетчика (для Free)
        /// </summary>
        [BsonElement("resetDateUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ResetDateUtc { get; set; }

        /// <summary>
        /// Активна ли подписка
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Автопродление
        /// </summary>
        [BsonElement("autoRenew")]
        public bool AutoRenew { get; set; } = false;

        /// <summary>
        /// ID транзакции оплаты
        /// </summary>
        [BsonElement("paymentId")]
        public string? PaymentId { get; set; }

        // ✅ ДОБАВЛЕНО: Ссылка на тарифный план
        /// <summary>
        /// ID тарифного плана (для получения параметров)
        /// </summary>
        [BsonElement("planId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId? PlanId { get; set; }

        // ✅ ДОБАВЛЕНО: Для отслеживания статуса
        /// <summary>
        /// Статус подписки
        /// </summary>
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    }        
}

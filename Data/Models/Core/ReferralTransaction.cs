using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Транзакция в реферальной программе (начисление или вывод).
    /// </summary>
    public class ReferralTransaction : BaseEntity
    {
        /// <summary>
        /// Telegram UserId владельца, которому начислено/списано.
        /// </summary>
        [BsonElement("ownerTgUserId")]
        public long OwnerTgUserId { get; set; }

        /// <summary>
        /// Telegram UserId реферала, совершившего платёж (если применимо).
        /// </summary>
        [BsonElement("referredTgUserId")]
        public long? ReferredTgUserId { get; set; }

        /// <summary>
        /// ID зеркала, через которое пришёл платёж (если применимо).
        /// </summary>
        [BsonElement("mirrorBotId")]
        public ObjectId? MirrorBotId { get; set; }

        /// <summary>
        /// Сумма транзакции (в минимальных единицах валюты, например копейки).
        /// Положительное значение = начисление, отрицательное = вывод.
        /// </summary>
        [BsonElement("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Валюта транзакции.
        /// </summary>
        [BsonElement("currency")]
        public string Currency { get; set; } = "RUB";

        /// <summary>
        /// Тип операции.
        /// </summary>
        [BsonElement("kind")]
        [BsonRepresentation(BsonType.String)]
        public ReferralTransactionKind Kind { get; set; }

        /// <summary>
        /// ID платежа из системы оплаты (YooMoney и т.п.).
        /// </summary>
        [BsonElement("paymentId")]
        public string? PaymentId { get; set; }

        /// <summary>
        /// Источник платежа (YooMoney, Test и т.п.).
        /// </summary>
        [BsonElement("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Комментарий/описание транзакции.
        /// </summary>
        [BsonElement("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Тип реферальной транзакции.
    /// </summary>
    public enum ReferralTransactionKind
    {
        /// <summary>
        /// Начисление от платежа реферала.
        /// </summary>
        Accrual,

        /// <summary>
        /// Вывод средств владельцем.
        /// </summary>
        Payout,

        /// <summary>
        /// Корректировка баланса (админ).
        /// </summary>
        Adjustment
    }
}

using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Статистика реферальной программы для владельца зеркала.
    /// </summary>
    public class ReferralStats : BaseEntity
    {
        /// <summary>
        /// Telegram UserId владельца.
        /// </summary>
        [BsonElement("ownerTgUserId")]
        public long OwnerTgUserId { get; set; }

        /// <summary>
        /// Общее количество рефералов (всех, кто присоединился через любое его зеркало).
        /// </summary>
        [BsonElement("totalReferrals")]
        public int TotalReferrals { get; set; }

        /// <summary>
        /// Количество рефералов, которые хотя бы раз совершили платёж.
        /// </summary>
        [BsonElement("paidReferrals")]
        public int PaidReferrals { get; set; }

        /// <summary>
        /// Суммарный доход от всех рефералов за всё время (в минимальных единицах валюты, например копейки).
        /// </summary>
        [BsonElement("totalReferralRevenue")]
        public decimal TotalReferralRevenue { get; set; }

        /// <summary>
        /// Текущий баланс, доступный к выводу (в минимальных единицах).
        /// </summary>
        [BsonElement("balance")]
        public decimal Balance { get; set; }

        /// <summary>
        /// Валюта баланса (например, "RUB", "USD").
        /// </summary>
        [BsonElement("currency")]
        public string Currency { get; set; } = "RUB";

        /// <summary>
        /// Дата последнего обновления статистики (UTC).
        /// </summary>
        [BsonElement("lastUpdatedAtUtc")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.Core
{
    /// <summary>
    /// Настройки владельца зеркала (уведомления, реферальная программа и т.п.).
    /// </summary>
    public class MirrorBotOwnerSettings : BaseEntity
    {
        /// <summary>
        /// Telegram UserId владельца.
        /// </summary>
        [BsonElement("ownerTgUserId")]
        public long OwnerTgUserId { get; set; }

        /// <summary>
        /// Уведомлять при присоединении нового пользователя к боту.
        /// </summary>
        [BsonElement("notifyOnNewReferral")]
        public bool NotifyOnNewReferral { get; set; } = true;

        /// <summary>
        /// Уведомлять при пополнении реферального баланса.
        /// </summary>
        [BsonElement("notifyOnReferralEarnings")]
        public bool NotifyOnReferralEarnings { get; set; } = true;

        /// <summary>
        /// Уведомлять при выводе средств.
        /// </summary>
        [BsonElement("notifyOnPayout")]
        public bool NotifyOnPayout { get; set; } = true;

        /// <summary>
        /// Telegram ChatId для отправки уведомлений (обычно = OwnerTgUserId).
        /// </summary>
        [BsonElement("notificationChatId")]
        public long NotificationChatId { get; set; }
    }
}

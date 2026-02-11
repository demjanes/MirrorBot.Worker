using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.Subscription
{
    /// <summary>
    /// Информация о подписке для отображения пользователю.
    /// </summary>
    public class SubscriptionInfo
    {
        public SubscriptionType Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public bool IsPremium => Type != SubscriptionType.Free;
        public DateTime? ExpiresAt { get; set; }
        public int DaysRemaining { get; set; }
        public int DailyTextLimit { get; set; }
        public int DailyVoiceLimit { get; set; }
        public int TextMessagesUsedToday { get; set; }
        public int VoiceMessagesUsedToday { get; set; }
        public bool VoiceResponseEnabled { get; set; }
        public bool GrammarCorrectionEnabled { get; set; }
        public bool VocabularyTrackingEnabled { get; set; }
    }
}

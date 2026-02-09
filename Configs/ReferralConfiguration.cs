using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Configs
{
    /// <summary>
    /// Конфигурация реферальной программы.
    /// </summary>
    public class ReferralConfiguration
    {
        public const string SectionName = "Referral";

        /// <summary>
        /// Процент от платежа, который получает реферер (0.0 - 1.0, например 0.15 = 15%).
        /// </summary>
        public decimal ReferralPercentage { get; set; } = 0.15m;

        /// <summary>
        /// Минимальная сумма для вывода средств.
        /// </summary>
        public decimal MinimumPayoutAmount { get; set; } = 500m;

        /// <summary>
        /// Валюта по умолчанию.
        /// </summary>
        public string DefaultCurrency { get; set; } = "RUB";
    }
}

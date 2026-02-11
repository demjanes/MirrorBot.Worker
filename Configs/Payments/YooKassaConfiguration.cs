using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Configs.Payments
{
    public sealed class YooKassaConfiguration
    {
        public const string SectionName = "YooKassa";

        /// <summary>
        /// ID магазина в ЮКассе.
        /// </summary>
        public string ShopId { get; set; } = string.Empty;

        /// <summary>
        /// Секретный ключ для API.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;
                             
        /// <summary>
        /// Включен ли тестовый режим.
        /// </summary>
        public bool TestMode { get; set; } = true;
    }
}

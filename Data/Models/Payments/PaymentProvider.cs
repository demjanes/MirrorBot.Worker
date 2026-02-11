using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.Payments
{
    /// <summary>
    /// Провайдер платежа.
    /// </summary>
    public enum PaymentProvider
    {
        /// <summary>
        /// ЮКасса (YooMoney).
        /// </summary>
        YooKassa,

        /// <summary>
        /// Stripe.
        /// </summary>
        Stripe,

        /// <summary>
        /// Криптовалюта.
        /// </summary>
        Crypto,

        /// <summary>
        /// Telegram Stars.
        /// </summary>
        TelegramStars,

        /// <summary>
        /// Тестовый провайдер.
        /// </summary>
        Test
    }
}

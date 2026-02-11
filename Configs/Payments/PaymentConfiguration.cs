using MirrorBot.Worker.Data.Models.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Configs.Payments
{
    public class PaymentConfiguration
    {
        public const string SectionName = "Payment";

        /// <summary>
        /// Провайдер по умолчанию.
        /// </summary>
        public PaymentProvider DefaultProvider { get; set; } = PaymentProvider.YooKassa;               
    }
}

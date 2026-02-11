using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Models.Payments
{
    /// <summary>
    /// Статус платежа.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// Создан, ожидает оплаты.
        /// </summary>
        Pending,

        /// <summary>
        /// Успешно оплачен.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Отменен.
        /// </summary>
        Canceled,

        /// <summary>
        /// Ошибка при оплате.
        /// </summary>
        Failed
    }
}

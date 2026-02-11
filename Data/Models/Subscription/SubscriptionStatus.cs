namespace MirrorBot.Worker.Data.Models.Subscription
{
    /// <summary>
    /// Статус подписки
    /// </summary>
    public enum SubscriptionStatus
    {
        Active = 0,       // Активна
        Expired = 1,      // Истекла
        Canceled = 2,     // Отменена
        PaymentFailed = 3 // Ошибка оплаты
    }
}

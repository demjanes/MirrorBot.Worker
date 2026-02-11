namespace MirrorBot.Worker.Data.Models.Subscription
{
    /// <summary>
    /// Типы подписок
    /// </summary>
    public enum SubscriptionType
    {
        /// <summary>
        /// Бесплатный (ограниченный)
        /// </summary>
        Free = 0,

        /// <summary>
        /// Premium - 1 месяц
        /// </summary>
        PremiumMonthly = 1,

        /// <summary>
        /// Premium - 3 месяца
        /// </summary>
        PremiumQuarterly = 3,

        /// <summary>
        /// Premium - 6 месяцев
        /// </summary>
        PremiumHalfYear = 6,

        /// <summary>
        /// Premium - 12 месяцев
        /// </summary>
        PremiumYearly = 12
    }
}

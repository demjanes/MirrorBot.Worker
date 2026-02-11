using MirrorBot.Worker.Data.Models.Subscription;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с тарифными планами.
    /// </summary>
    public interface ISubscriptionPlanRepository : IBaseRepository<SubscriptionPlan>
    {
        /// <summary>
        /// Получить тарифный план по типу подписки.
        /// </summary>
        Task<SubscriptionPlan?> GetByTypeAsync(
            SubscriptionType type,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить все активные тарифные планы.
        /// </summary>
        Task<List<SubscriptionPlan>> GetActiveRatesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить только Premium планы.
        /// </summary>
        Task<List<SubscriptionPlan>> GetPremiumPlansAsync(
            CancellationToken cancellationToken = default);
    }
}

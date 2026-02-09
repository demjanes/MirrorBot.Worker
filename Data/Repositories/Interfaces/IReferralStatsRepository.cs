using MirrorBot.Worker.Data.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы со статистикой реферальной программы.
    /// </summary>
    public interface IReferralStatsRepository : IBaseRepository<ReferralStats>
    {
        /// <summary>
        /// Получить статистику владельца по TgUserId.
        /// </summary>
        Task<ReferralStats?> GetByOwnerTgUserIdAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создать или получить существующую статистику владельца.
        /// </summary>
        Task<ReferralStats> GetOrCreateAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Инкрементировать количество рефералов.
        /// </summary>
        Task IncrementTotalReferralsAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Инкрементировать количество платящих рефералов.
        /// </summary>
        Task IncrementPaidReferralsAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавить сумму к балансу и общему доходу.
        /// </summary>
        Task AddEarningsAsync(
            long ownerTgUserId,
            decimal amount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Вычесть сумму из баланса (вывод средств).
        /// </summary>
        Task DeductBalanceAsync(
            long ownerTgUserId,
            decimal amount,
            CancellationToken cancellationToken = default);
    }
}

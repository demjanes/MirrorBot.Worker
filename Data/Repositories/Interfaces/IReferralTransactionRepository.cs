using MirrorBot.Worker.Data.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с транзакциями реферальной программы.
    /// </summary>
    public interface IReferralTransactionRepository : IBaseRepository<ReferralTransaction>
    {
        /// <summary>
        /// Получить последние транзакции владельца.
        /// </summary>
        Task<List<ReferralTransaction>> GetOwnerTransactionsAsync(
            long ownerTgUserId,
            int limit = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создать новую транзакцию.
        /// </summary>
        Task CreateAsync(
            ReferralTransaction transaction,
            CancellationToken cancellationToken = default);
    }
}

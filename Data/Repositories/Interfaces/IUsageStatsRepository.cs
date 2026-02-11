using MirrorBot.Worker.Data.Models.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы со статистикой использования.
    /// </summary>
    public interface IUsageStatsRepository : IBaseRepository<UsageStats>
    {
        /// <summary>
        /// Получить статистику за сегодня.
        /// </summary>
        Task<UsageStats?> GetTodayStatsAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Инкрементировать счетчик текстовых сообщений.
        /// </summary>
        Task IncrementTextMessagesAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Инкрементировать счетчик голосовых сообщений.
        /// </summary>
        Task IncrementVoiceMessagesAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавить использованные токены.
        /// </summary>
        Task AddTokensUsedAsync(
            long userId,
            int tokensUsed,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить статистику за период.
        /// </summary>
        Task<List<UsageStats>> GetStatsForPeriodAsync(
            long userId,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default);
    }
}

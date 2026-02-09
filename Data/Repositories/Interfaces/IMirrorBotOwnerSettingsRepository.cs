using MirrorBot.Worker.Data.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий настроек владельцев зеркал.
    /// </summary>
    public interface IMirrorBotOwnerSettingsRepository : IBaseRepository<MirrorBotOwnerSettings>
    {
        /// <summary>
        /// Получить настройки владельца.
        /// </summary>
        Task<MirrorBotOwnerSettings?> GetByOwnerTgUserIdAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить или создать настройки владельца с дефолтными значениями.
        /// </summary>
        Task<MirrorBotOwnerSettings> GetOrCreateAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить настройки уведомлений.
        /// </summary>
        Task UpdateNotificationSettingsAsync(
            long ownerTgUserId,
            bool? notifyOnNewReferral,
            bool? notifyOnReferralEarnings,
            bool? notifyOnPayout,
            CancellationToken cancellationToken = default);
    }
}

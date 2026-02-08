using MirrorBot.Worker.Data.Models.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с ботами-зеркалами
    /// </summary>
    public interface IMirrorBotsRepository : IBaseRepository<Models.Core.BotMirror>
    {
        /// <summary>
        /// Получить все активные боты
        /// </summary>
        Task<List<Models.Core.BotMirror>> GetEnabledAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Найти бота по зашифрованному токену
        /// </summary>
        Task<Models.Core.BotMirror?> GetByEncryptedTokenAsync(
            string encryptedToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Найти бота по хешу токена
        /// </summary>
        Task<Models.Core.BotMirror?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить ботов владельца
        /// </summary>
        Task<List<Models.Core.BotMirror>> GetByOwnerTgIdAsync(
            long ownerTgId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Подсчитать количество ботов владельца
        /// </summary>
        Task<long> CountByOwnerTgIdAsync(
            long ownerTgId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить статус здоровья бота
        /// </summary>
        Task<bool> UpdateHealthAsync(
            ObjectId botId,
            DateTime lastSeenUtc,
            string? lastError,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Включить/выключить бота
        /// </summary>
        Task<Models.Core.BotMirror?> SetEnabledAsync(
            ObjectId botId,
            bool isEnabled,
            DateTime nowUtc,
            CancellationToken cancellationToken = default);
    }
}

using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Events;
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
    /// Репозиторий для работы с пользователями
    /// </summary>
    public interface IUsersRepository : IBaseRepository<User>
    {
        /// <summary>
        /// Получить пользователя по Telegram ID
        /// </summary>
        Task<User?> GetByTelegramIdAsync(
            long telegramUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Установить реферала, если еще не установлен
        /// </summary>
        Task<bool> SetReferralIfEmptyAsync(
            long telegramUserId,
            long ownerId,
            ObjectId mirrorBotId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Потоковое чтение пользователей для рассылки
        /// </summary>
        IAsyncEnumerable<User> StreamForBroadcastAsync(
            DateTime activeAfterUtc,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Пометить пользователя как недоступного для отправки
        /// </summary>
        Task<bool> MarkCantSendLastBotAsync(
            long telegramUserId,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Upsert пользователя при взаимодействии
        /// </summary>
       Task<(User User, bool IsNewUser)> UpsertSeenAsync(
             UserSeenEvent e,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить предпочитаемый язык пользователя
        /// </summary>
        Task<UiLang> GetPreferredLangAsync(
            long tgUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Установить предпочитаемый язык
        /// </summary>
        Task<User?> SetPreferredLangAsync(
            long tgUserId,
            UiLang lang,
            DateTime nowUtc,
            CancellationToken cancellationToken = default);
    }
}

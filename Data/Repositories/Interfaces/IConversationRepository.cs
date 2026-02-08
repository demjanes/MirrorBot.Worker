using MirrorBot.Worker.Data.Models.English;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с диалогами
    /// </summary>
    public interface IConversationRepository : IBaseRepository<Conversation>
    {
        /// <summary>
        /// Получить активный диалог пользователя с ботом
        /// </summary>
        Task<Conversation?> GetActiveConversationAsync(
            long userId,
            string botId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить все диалоги пользователя
        /// </summary>
        Task<List<Conversation>> GetUserConversationsAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавить сообщение в диалог
        /// </summary>
        Task<bool> AddMessageAsync(
            ObjectId conversationId,
            EnglishMessage message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить режим диалога
        /// </summary>
        Task<bool> UpdateModeAsync(
            ObjectId conversationId,
            string mode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Закрыть диалог (деактивировать)
        /// </summary>
        Task<bool> CloseConversationAsync(
            ObjectId conversationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить последние N сообщений из диалога
        /// </summary>
        Task<List<EnglishMessage>> GetRecentMessagesAsync(
            ObjectId conversationId,
            int count,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить время последней активности
        /// </summary>
        Task<bool> UpdateLastActivityAsync(
            ObjectId conversationId,
            CancellationToken cancellationToken = default);
    }
}

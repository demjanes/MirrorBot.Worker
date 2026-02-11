using MirrorBot.Worker.Data.Models.English;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    public interface IConversationRepository : IBaseRepository<Conversation>
    {
        /// <summary>
        /// Получить единый контекст пользователя (независимо от бота)
        /// </summary>
        Task<Conversation?> GetByUserAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создать или обновить контекст
        /// </summary>
        Task<Conversation> CreateOrUpdateAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default);
    }
}

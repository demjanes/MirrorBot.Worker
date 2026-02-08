using MirrorBot.Worker.Data.Models.English;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{

    /// <summary>
    /// Репозиторий для работы со словарем пользователя
    /// </summary>
    public interface IVocabularyRepository : IBaseRepository<UserVocabulary>
    {
        /// <summary>
        /// Получить словарь пользователя
        /// </summary>
        Task<UserVocabulary?> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавить слово в словарь
        /// </summary>
        Task<bool> AddWordAsync(
            long userId,
            VocabularyWord word,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Удалить слово из словаря
        /// </summary>
        Task<bool> RemoveWordAsync(
            long userId,
            string word,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить статистику повторения слова
        /// </summary>
        Task<bool> UpdateWordReviewAsync(
            long userId,
            string word,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить слова для повторения
        /// </summary>
        Task<List<VocabularyWord>> GetWordsForReviewAsync(
            long userId,
            int count,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Поиск слова в словаре
        /// </summary>
        Task<VocabularyWord?> FindWordAsync(
            long userId,
            string word,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить количество слов в словаре
        /// </summary>
        Task<int> GetVocabularySizeAsync(
            long userId,
            CancellationToken cancellationToken = default);
    }
}

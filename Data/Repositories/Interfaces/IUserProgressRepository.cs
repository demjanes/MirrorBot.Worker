using MirrorBot.Worker.Data.Models.English;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с прогрессом пользователя
    /// </summary>
    public interface IUserProgressRepository : IBaseRepository<UserProgress>
    {
        /// <summary>
        /// Получить прогресс пользователя
        /// </summary>
        Task<UserProgress?> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Увеличить счетчик сообщений
        /// </summary>
        Task<bool> IncrementMessagesAsync(
            long userId,
            bool isVoice,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавить исправления
        /// </summary>
        Task<bool> AddCorrectionsAsync(
            long userId,
            int count,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить размер словаря
        /// </summary>
        Task<bool> UpdateVocabularySizeAsync(
            long userId,
            int size,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить оценку произношения
        /// </summary>
        Task<bool> UpdatePronunciationScoreAsync(
            long userId,
            int score,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить streak (дни подряд)
        /// </summary>
        Task<bool> UpdateStreakAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить уровень пользователя
        /// </summary>
        Task<bool> UpdateLevelAsync(
            long userId,
            string level,
            CancellationToken cancellationToken = default);
    }
}

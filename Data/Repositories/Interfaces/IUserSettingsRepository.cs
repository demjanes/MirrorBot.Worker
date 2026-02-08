using MirrorBot.Worker.Data.Models.English;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с настройками пользователя
    /// </summary>
    public interface IUserSettingsRepository : IBaseRepository<UserSettings>
    {
        /// <summary>
        /// Получить настройки пользователя
        /// </summary>
        Task<UserSettings?> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создать настройки по умолчанию
        /// </summary>
        Task<UserSettings> CreateDefaultAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить голос для TTS
        /// </summary>
        Task<bool> UpdateVoiceAsync(
            long userId,
            string voice,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить скорость речи
        /// </summary>
        Task<bool> UpdateSpeechSpeedAsync(
            long userId,
            double speed,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Переключить автоответы голосом
        /// </summary>
        Task<bool> ToggleAutoVoiceResponseAsync(
            long userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить режим по умолчанию
        /// </summary>
        Task<bool> UpdateDefaultModeAsync(
            long userId,
            string mode,
            CancellationToken cancellationToken = default);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Interfaces
{
    public interface IResponseCache
    {
        /// <summary>
        /// Получить закэшированный ответ
        /// </summary>
        Task<string> GetAsync(string key);

        /// <summary>
        /// Сохранить ответ в кэш
        /// </summary>
        Task SetAsync(string key, string value, TimeSpan? expiration = null);

        /// <summary>
        /// Проверить наличие ключа в кэше
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Удалить из кэша
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Очистить весь кэш
        /// </summary>
        Task ClearAsync();
    }
}

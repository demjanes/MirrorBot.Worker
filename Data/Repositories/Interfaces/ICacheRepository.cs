using MirrorBot.Worker.Data.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Репозиторий для работы с кэшем ответов ИИ.
    /// </summary>
    public interface ICacheRepository : IBaseRepository<CachedResponse>
    {
        /// <summary>
        /// Получить кэшированный ответ по ключу кэша.
        /// </summary>
        Task<CachedResponse?> GetByCacheKeyAsync(
            string cacheKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохранить (upsert) кэшированный ответ.
        /// </summary>
        Task SaveAsync(
            CachedResponse cachedResponse,
            CancellationToken cancellationToken = default);
    }
}

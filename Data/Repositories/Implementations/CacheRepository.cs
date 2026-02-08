using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    /// <summary>
    /// Mongo-репозиторий кэша ответов ИИ.
    /// </summary>
    public class CacheRepository : BaseRepository<CachedResponse>, ICacheRepository
    {
        private const string CollectionName = "cached_responses";

        public CacheRepository(IMongoDatabase database)
            : base(database, CollectionName)
        {
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            // TTL-индекс по expiresAtUtc
            var ttlIndexKeys = Builders<CachedResponse>.IndexKeys
                .Ascending(x => x.ExpiresAtUtc);

            var ttlIndexOptions = new CreateIndexOptions
            {
                Name = "ttl_expiresAtUtc",
                ExpireAfter = TimeSpan.Zero
            };

            var ttlIndexModel = new CreateIndexModel<CachedResponse>(
                ttlIndexKeys,
                ttlIndexOptions);

            _collection.Indexes.CreateOne(ttlIndexModel);

            // Уникальный индекс по cacheKey
            var keyIndexKeys = Builders<CachedResponse>.IndexKeys
                .Ascending(x => x.CacheKey);

            var keyIndexOptions = new CreateIndexOptions
            {
                Name = "ux_cacheKey",
                Unique = true
            };

            var keyIndexModel = new CreateIndexModel<CachedResponse>(
                keyIndexKeys,
                keyIndexOptions);

            _collection.Indexes.CreateOne(keyIndexModel);
        }

        public async Task<CachedResponse?> GetByCacheKeyAsync(
            string cacheKey,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<CachedResponse>.Filter
                .Eq(x => x.CacheKey, cacheKey);

            return await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task SaveAsync(
            CachedResponse cachedResponse,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<CachedResponse>.Filter
                .Eq(x => x.CacheKey, cachedResponse.CacheKey);

            await _collection.ReplaceOneAsync(
                filter,
                cachedResponse,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }
}

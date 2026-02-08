using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    /// <summary>
    /// Репозиторий для работы с ботами-зеркалами
    /// </summary>
    public sealed class MirrorBotsRepository : BaseRepository<BotMirror>, IMirrorBotsRepository
    {
        public MirrorBotsRepository(IMongoDatabase database)
            : base(database, MongoCollections.MirrorBots)
        {
            CreateIndexes();
        }

        /// <summary>
        /// Создание индексов для оптимизации запросов
        /// </summary>
        private void CreateIndexes()
        {
            // Индекс по владельцу
            var ownerIndex = Builders<BotMirror>.IndexKeys
                .Ascending(x => x.OwnerTelegramUserId);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<BotMirror>(ownerIndex));

            // Индекс по токену (уникальный)
            var tokenHashIndex = Builders<BotMirror>.IndexKeys
                .Ascending(x => x.TokenHash);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<BotMirror>(
                    tokenHashIndex,
                    new CreateIndexOptions { Unique = true }));

            // Индекс по зашифрованному токену (уникальный)
            var encryptedTokenIndex = Builders<BotMirror>.IndexKeys
                .Ascending(x => x.EncryptedToken);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<BotMirror>(
                    encryptedTokenIndex,
                    new CreateIndexOptions { Unique = true }));

            // Индекс по статусу активности
            var enabledIndex = Builders<BotMirror>.IndexKeys
                .Ascending(x => x.IsEnabled)
                .Descending(x => x.LastSeenAtUtc);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<BotMirror>(enabledIndex));

            // Индекс по username бота
            var usernameIndex = Builders<BotMirror>.IndexKeys
                .Ascending(x => x.BotUsername);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<BotMirror>(usernameIndex));
        }

        // ============================================
        // СПЕЦИФИЧНЫЕ МЕТОДЫ
        // ============================================

        /// <summary>
        /// Получить все активные боты
        /// </summary>
        public Task<List<BotMirror>> GetEnabledAsync(
            CancellationToken cancellationToken = default)
        {
            return _collection
                .Find(x => x.IsEnabled)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Найти бота по зашифрованному токену
        /// </summary>
        public Task<Models.Core.BotMirror?> GetByEncryptedTokenAsync(
            string encryptedToken,
            CancellationToken cancellationToken = default)
        {
            return _collection
                .Find(x => x.EncryptedToken == encryptedToken)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Найти бота по хешу токена
        /// </summary>
        public Task<BotMirror?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return _collection
                .Find(x => x.TokenHash == tokenHash)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Получить ботов владельца
        /// </summary>
        public Task<List<BotMirror>> GetByOwnerTgIdAsync(
            long ownerTgId,
            CancellationToken cancellationToken = default)
        {
            return _collection
                .Find(x => x.OwnerTelegramUserId == ownerTgId)
                .SortByDescending(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Подсчитать количество ботов владельца
        /// </summary>
        public Task<long> CountByOwnerTgIdAsync(
            long ownerTgId,
            CancellationToken cancellationToken = default)
        {
            return _collection.CountDocumentsAsync(
                x => x.OwnerTelegramUserId == ownerTgId,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Обновить статус здоровья бота
        /// </summary>
        public async Task<bool> UpdateHealthAsync(
            ObjectId botId,
            DateTime lastSeenUtc,
            string? lastError,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<BotMirror>.Update
                .Set(x => x.LastSeenAtUtc, lastSeenUtc)
                .Set(x => x.LastError, lastError);

            var result = await _collection.UpdateOneAsync(
                x => x.Id == botId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Включить/выключить бота
        /// </summary>
        public async Task<Models.Core.BotMirror?> SetEnabledAsync(
            ObjectId botId,
            bool isEnabled,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<BotMirror>.Filter.Eq(x => x.Id, botId);

            var update = Builders<BotMirror>.Update.Combine(
                Builders<BotMirror>.Update.Set(x => x.IsEnabled, isEnabled),
                Builders<BotMirror>.Update.Set(x => x.LastSeenAtUtc, nowUtc),
                Builders<BotMirror>.Update.Set(x => x.LastError, null)
            );

            var options = new FindOneAndUpdateOptions<Models.Core.BotMirror>
            {
                ReturnDocument = ReturnDocument.After
            };

            return await _collection.FindOneAndUpdateAsync(
                filter,
                update,
                options,
                cancellationToken);
        }
    }
}

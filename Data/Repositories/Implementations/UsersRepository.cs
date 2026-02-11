using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    /// <summary>
    /// Репозиторий для работы с пользователями
    /// </summary>
    public sealed class UsersRepository : BaseRepository<User>, IUsersRepository
    {
        public UsersRepository(IMongoDatabase database)
            : base(database, MongoCollections.Users)
        {
            CreateIndexes();
        }

        /// <summary>
        /// Создание индексов для оптимизации запросов
        /// </summary>
        private void CreateIndexes()
        {
            // Уникальный индекс по Telegram ID
            var tgUserIdIndex = Builders<User>.IndexKeys
                .Ascending(x => x.TgUserId);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<User>(
                    tgUserIdIndex,
                    new CreateIndexOptions { Unique = true }));

            // Индекс по username
            var usernameIndex = Builders<User>.IndexKeys
                .Ascending(x => x.TgUsername);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<User>(usernameIndex));

            // Составной индекс для рассылки
            var broadcastIndex = Builders<User>.IndexKeys
                .Descending(x => x.LastMessageAtUtc)
                .Ascending(x => x.CanSendLastBot)
                .Ascending(x => x.LastBotKey);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<User>(broadcastIndex));

            // Индекс по рефереру
            var referrerIndex = Builders<User>.IndexKeys
                .Ascending(x => x.ReferrerOwnerTgUserId)
                .Ascending(x => x.ReferrerMirrorBotId);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<User>(referrerIndex));

            // Индекс по языку
            var langIndex = Builders<User>.IndexKeys
                .Ascending(x => x.PreferredLang);
            _collection.Indexes.CreateOne(
                new CreateIndexModel<User>(langIndex));
        }

        // ============================================
        // СПЕЦИФИЧНЫЕ МЕТОДЫ
        // ============================================

        /// <summary>
        /// Получить пользователя по Telegram ID
        /// </summary>
        public Task<User?> GetByTelegramIdAsync(
            long telegramUserId,
            CancellationToken cancellationToken = default)
        {
            return _collection
                .Find(x => x.TgUserId == telegramUserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Установить реферала, если еще не установлен
        /// </summary>
        public async Task<bool> SetReferralIfEmptyAsync(
            long telegramUserId,
            long ownerId,
            ObjectId mirrorBotId,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<User>.Update
                .Set(x => x.ReferrerOwnerTgUserId, ownerId)
                .Set(x => x.ReferrerMirrorBotId, mirrorBotId);

            var result = await _collection.UpdateOneAsync(
                x => x.TgUserId == telegramUserId && x.ReferrerOwnerTgUserId == null,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Потоковое чтение пользователей для рассылки
        /// </summary>
        public async IAsyncEnumerable<User> StreamForBroadcastAsync(
            DateTime activeAfterUtc,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Ne(x => x.LastBotKey, null),
                Builders<User>.Filter.Ne(x => x.LastChatId, null),
                Builders<User>.Filter.Eq(x => x.CanSendLastBot, true),
                Builders<User>.Filter.Gte(x => x.LastMessageAtUtc, activeAfterUtc)
            );

            using var cursor = await _collection
                .Find(filter)
                .ToCursorAsync(cancellationToken);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var user in cursor.Current)
                {
                    yield return user;
                }
            }
        }

        /// <summary>
        /// Пометить пользователя как недоступного для отправки
        /// </summary>
        public async Task<bool> MarkCantSendLastBotAsync(
            long telegramUserId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<User>.Update
                .Set(x => x.CanSendLastBot, false)
                .Set(x => x.LastSendError, reason);

            var result = await _collection.UpdateOneAsync(
                x => x.TgUserId == telegramUserId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Upsert пользователя при взаимодействии
        /// </summary>
        public async Task<(User User, bool IsNewUser)> UpsertSeenAsync(
            UserSeenEvent e,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<User>.Filter.Eq(x => x.TgUserId, e.TgUserId);

            var updates = new List<UpdateDefinition<User>>
        {
            // Создаём при первом появлении
            Builders<User>.Update.SetOnInsert(x => x.TgUserId, e.TgUserId),
            Builders<User>.Update.SetOnInsert(x => x.CreatedAtUtc, e.SeenAtUtc),

            // Профиль — обновляем каждый раз
            Builders<User>.Update.Set(x => x.TgUsername, e.TgUsername),
            Builders<User>.Update.Set(x => x.TgFirstName, e.TgFirstName),
            Builders<User>.Update.Set(x => x.TgLastName, e.TgLastName),

            // Язык
            Builders<User>.Update.Set(x => x.TgLangCode, e.TgLangCode),

            // Last-active (для рассылки)
            Builders<User>.Update.Set(x => x.LastBotKey, e.LastBotKey),
            Builders<User>.Update.Set(x => x.LastChatId, e.LastChatId),
            Builders<User>.Update.Set(x => x.LastMessageAtUtc, e.SeenAtUtc),
            Builders<User>.Update.Set(x => x.CanSendLastBot, true),
            Builders<User>.Update.Set(x => x.LastSendError, null)
        };

            // Реферал заполняем только при создании
            updates.Add(Builders<User>.Update.SetOnInsert(
                x => x.ReferrerOwnerTgUserId, e.ReferrerOwnerTgUserId));
            updates.Add(Builders<User>.Update.SetOnInsert(
                x => x.ReferrerMirrorBotId, e.ReferrerMirrorBotId));

            var update = Builders<User>.Update.Combine(updates);

            var options = new FindOneAndUpdateOptions<User>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var user = await _collection.FindOneAndUpdateAsync(
                filter,
                update,
                options,
                cancellationToken);

            // ✅ КЛЮЧЕВАЯ ЛОГИКА: Если CreatedAtUtc == SeenAtUtc (с погрешностью 1 сек), значит пользователь только что создан
            var isNewUser = Math.Abs((user.CreatedAtUtc - e.SeenAtUtc).TotalSeconds) < 1;

            return (user, isNewUser);
        }

        /// <summary>
        /// Получить предпочитаемый язык пользователя
        /// </summary>
        public async Task<UiLang> GetPreferredLangAsync(
            long tgUserId,
            CancellationToken cancellationToken = default)
        {
            var projection = Builders<User>.Projection
                .Expression(x => x.PreferredLang);

            var result = await _collection
                .Find(x => x.TgUserId == tgUserId)
                .Project(projection)
                .FirstOrDefaultAsync(cancellationToken);

            return result != UiLang.Def ? result : UiLang.Ru;
        }

        /// <summary>
        /// Установить предпочитаемый язык
        /// </summary>
        public async Task<User?> SetPreferredLangAsync(
            long tgUserId,
            UiLang lang,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<User>.Filter.Eq(x => x.TgUserId, tgUserId);

            var update = Builders<User>.Update
                .Set(x => x.PreferredLang, lang)
                .Set(x => x.LastMessageAtUtc, nowUtc);

            var options = new FindOneAndUpdateOptions<User>
            {
                IsUpsert = true,
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

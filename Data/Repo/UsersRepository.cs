using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Events;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MirrorBot.Worker.Data.Repo
{
    public sealed class UsersRepository
    {
        private readonly IMongoCollection<UserEntity> _col;

        public UsersRepository(IMongoDatabase db)
            => _col = db.GetCollection<UserEntity>(MongoCollections.Users);

        public Task<UserEntity?> GetByTelegramIdAsync(long telegramUserId, CancellationToken ct)
            => _col.Find(x => x.TgUserId == telegramUserId).FirstOrDefaultAsync(ct);

        public Task InsertAsync(UserEntity entity, CancellationToken ct)
            => _col.InsertOneAsync(entity, cancellationToken: ct);

        public Task SetReferralIfEmptyAsync(long telegramUserId, long ownerId, ObjectId mirrorBotId, CancellationToken ct)
            => _col.UpdateOneAsync(
                x => x.TgUserId == telegramUserId && x.ReferrerOwnerTgUserId == null,
                Builders<UserEntity>.Update
                    .Set(x => x.ReferrerOwnerTgUserId, ownerId)
                    .Set(x => x.ReferrerMirrorBotId, mirrorBotId),
                cancellationToken: ct);


        public async IAsyncEnumerable<UserEntity> StreamForBroadcastAsync(
            DateTime activeAfterUtc,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            var filter = Builders<UserEntity>.Filter.And(
                Builders<UserEntity>.Filter.Ne(x => x.LastBotKey, null),
                Builders<UserEntity>.Filter.Ne(x => x.LastChatId, null),
                Builders<UserEntity>.Filter.Eq(x => x.CanSendLastBot, true),
                Builders<UserEntity>.Filter.Gte(x => x.LastMessageAtUtc, activeAfterUtc)
            );

            using var cursor = await _col.Find(filter).ToCursorAsync(ct);

            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var user in cursor.Current)
                    yield return user;
            }
        }

        public Task MarkCantSendLastBotAsync(long telegramUserId, string reason, CancellationToken ct)
            => _col.UpdateOneAsync(
                x => x.TgUserId == telegramUserId,
                Builders<UserEntity>.Update
                    .Set(x => x.CanSendLastBot, false)
                    .Set(x => x.LastSendError, reason),
                cancellationToken: ct);


        public async Task<UserEntity?> UpsertSeenAsync(UserSeenEvent e, CancellationToken ct)
        {
            var filter = Builders<UserEntity>.Filter.Eq(x => x.TgUserId, e.TgUserId);

            var updates = new List<UpdateDefinition<UserEntity>>
            {
                // создаём при первом появлении
                Builders<UserEntity>.Update.SetOnInsert(x => x.TgUserId, e.TgUserId),
                Builders<UserEntity>.Update.SetOnInsert(x => x.CreatedAtUtc, e.SeenAtUtc),

                // "профиль" — можно обновлять каждый раз
                Builders<UserEntity>.Update.Set(x => x.TgUsername, e.TgUsername),
                Builders<UserEntity>.Update.Set(x => x.TgFirstName, e.TgFirstName),
                Builders<UserEntity>.Update.Set(x => x.TgLastName, e.TgLastName),

                //язык
                Builders<UserEntity>.Update.Set(x => x.TgLangCode, e.TgLangCode),

                // last-active (для рассылки)
                Builders<UserEntity>.Update.Set(x => x.LastBotKey, e.LastBotKey),
                Builders<UserEntity>.Update.Set(x => x.LastChatId, e.LastChatId),
                Builders<UserEntity>.Update.Set(x => x.LastMessageAtUtc, e.SeenAtUtc),
                Builders<UserEntity>.Update.Set(x => x.CanSendLastBot, true),
                Builders<UserEntity>.Update.Set(x => x.LastSendError, null)
            };

            // правило 1: заполняем реферала сразу при первом взаимодействии и больше не меняем
            // -> только SetOnInsert
            updates.Add(Builders<UserEntity>.Update.SetOnInsert(x => x.ReferrerOwnerTgUserId, e.ReferrerOwnerTgUserId));
            updates.Add(Builders<UserEntity>.Update.SetOnInsert(x => x.ReferrerMirrorBotId, e.ReferrerMirrorBotId));

            var update = Builders<UserEntity>.Update.Combine(updates);

            return await _col.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<UserEntity>
                {
                    IsUpsert = true,
                    ReturnDocument = ReturnDocument.After, 
                },
                ct);
        }

        public async Task<UiLang> GetPreferredLangAsync(long tgUserId, CancellationToken ct)
        {
            var u = await _col.Find(x => x.TgUserId == tgUserId)
                              .Project(x => x.PreferredLang)
                              .FirstOrDefaultAsync(ct);
            return u;
        }

        public async Task<UserEntity?> SetPreferredLangAsync(long tgUserId, UiLang lang, DateTime nowUtc, CancellationToken ct)
        {    
            var filter = Builders<UserEntity>.Filter.Eq(x => x.TgUserId, tgUserId);

            var update = Builders<UserEntity>.Update
                .Set(x => x.PreferredLang, lang)
                .Set(x => x.LastMessageAtUtc, nowUtc);

            var options = new FindOneAndUpdateOptions<UserEntity>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            return await _col.FindOneAndUpdateAsync(filter, update, options, ct);
        }
    }
}

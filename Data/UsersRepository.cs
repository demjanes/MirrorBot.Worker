using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data
{
    public sealed class UsersRepository
    {
        private readonly IMongoCollection<UserEntity> _col;

        public UsersRepository(IMongoDatabase db)
            => _col = db.GetCollection<UserEntity>(MongoCollections.Users);

        public Task<UserEntity?> GetByTelegramIdAsync(long telegramUserId, CancellationToken ct)
            => _col.Find(x => x.TelegramUserId == telegramUserId).FirstOrDefaultAsync(ct);

        public Task InsertAsync(UserEntity entity, CancellationToken ct)
            => _col.InsertOneAsync(entity, cancellationToken: ct);

        public Task SetReferralIfEmptyAsync(long telegramUserId, long ownerId, MongoDB.Bson.ObjectId mirrorBotId, CancellationToken ct)
            => _col.UpdateOneAsync(
                x => x.TelegramUserId == telegramUserId && x.ReferrerOwnerTelegramUserId == null,
                Builders<UserEntity>.Update
                    .Set(x => x.ReferrerOwnerTelegramUserId, ownerId)
                    .Set(x => x.ReferrerMirrorBotId, mirrorBotId),
                cancellationToken: ct);
    }
}

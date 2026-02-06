using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MirrorBot.Worker.Data.Repo
{
    public sealed class MirrorBotsRepository
    {
        private readonly IMongoCollection<MirrorBotEntity> _col;

        public MirrorBotsRepository(IMongoDatabase db)
            => _col = db.GetCollection<MirrorBotEntity>(MongoCollections.MirrorBots);

        public Task<List<MirrorBotEntity>> GetEnabledAsync(CancellationToken ct)
            => _col.Find(x => x.IsEnabled).ToListAsync(ct);

        public Task<MirrorBotEntity?> GetByTokenAsync(string token, CancellationToken ct)
            => _col.Find(x => x.Token == token).FirstOrDefaultAsync(ct);

        public Task<DeleteResult> DeleteByOdjectIdAsync(ObjectId id, CancellationToken ct)
            => _col.DeleteOneAsync(x => x.Id == id, ct);

        public Task<MirrorBotEntity?> GetByOdjectIdAsync(ObjectId botId, CancellationToken ct)
           => _col.Find(x => x.Id == botId).FirstOrDefaultAsync(ct);

        public Task<List<MirrorBotEntity>> GetByOwnerTgIdAsync(long ownerTgId, CancellationToken ct)
            => _col.Find(x => x.OwnerTelegramUserId == ownerTgId).ToListAsync(ct);

        public async Task<MirrorBotEntity> InsertAsync(MirrorBotEntity entity, CancellationToken ct)
        {
            await _col.InsertOneAsync(entity, cancellationToken: ct);
            return entity;
        }

        public Task UpdateHealthAsync(MirrorBotEntity bot, DateTime lastSeenUtc, string? lastError, CancellationToken ct)
            => _col.UpdateOneAsync(
                x => x.Id == bot.Id,
                Builders<MirrorBotEntity>.Update
                    .Set(x => x.LastSeenAtUtc, lastSeenUtc)
                    .Set(x => x.LastError, lastError),
                cancellationToken: ct);

        public async Task<MirrorBotEntity?> SetEnabledAsync(ObjectId botId, bool isEnabled, DateTime nowUtc, CancellationToken ct)
        {
            var filter = Builders<MirrorBotEntity>.Filter.Eq(x => x.Id, botId);

            var update = Builders<MirrorBotEntity>.Update.Combine(
                Builders<MirrorBotEntity>.Update.Set(x => x.IsEnabled, isEnabled),
                Builders<MirrorBotEntity>.Update.Set(x => x.LastSeenAtUtc, nowUtc),
                Builders<MirrorBotEntity>.Update.Set(x => x.LastError, null)
            );
                        
            var options = new FindOneAndUpdateOptions<MirrorBotEntity>
            {
                ReturnDocument = ReturnDocument.After
            };

            return await _col.FindOneAndUpdateAsync(filter, update, options, ct);
        }
    }
}

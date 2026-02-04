using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data
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
    }
}

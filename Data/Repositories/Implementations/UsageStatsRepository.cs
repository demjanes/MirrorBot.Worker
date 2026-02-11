using MirrorBot.Worker.Data.Models.Subscription;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    public class UsageStatsRepository : BaseRepository<UsageStats>, IUsageStatsRepository
    {
        public UsageStatsRepository(IMongoDatabase database)
            : base(database, "usage_stats")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var userDateIndex = Builders<UsageStats>.IndexKeys
                .Ascending(x => x.UserId)
                .Descending(x => x.Date);

            var indexOptions = new CreateIndexOptions
            {
                Unique = true,
                Name = "ux_userId_date"
            };

            _collection.Indexes.CreateOne(
                new CreateIndexModel<UsageStats>(userDateIndex, indexOptions));
        }

        public async Task<UsageStats?> GetTodayStatsAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return await _collection
                .Find(x => x.UserId == userId && x.Date == today)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task IncrementTextMessagesAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var filter = Builders<UsageStats>.Filter.And(
                Builders<UsageStats>.Filter.Eq(x => x.UserId, userId),
                Builders<UsageStats>.Filter.Eq(x => x.Date, today)
            );

            var update = Builders<UsageStats>.Update
                .Inc(x => x.TextMessagesCount, 1)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow)
                .SetOnInsert(x => x.UserId, userId)
                .SetOnInsert(x => x.Date, today)
                .SetOnInsert(x => x.CreatedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }

        public async Task IncrementVoiceMessagesAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var filter = Builders<UsageStats>.Filter.And(
                Builders<UsageStats>.Filter.Eq(x => x.UserId, userId),
                Builders<UsageStats>.Filter.Eq(x => x.Date, today)
            );

            var update = Builders<UsageStats>.Update
                .Inc(x => x.VoiceMessagesCount, 1)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow)
                .SetOnInsert(x => x.UserId, userId)
                .SetOnInsert(x => x.Date, today)
                .SetOnInsert(x => x.CreatedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }

        public async Task AddTokensUsedAsync(
            long userId,
            int tokensUsed,
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var filter = Builders<UsageStats>.Filter.And(
                Builders<UsageStats>.Filter.Eq(x => x.UserId, userId),
                Builders<UsageStats>.Filter.Eq(x => x.Date, today)
            );

            var update = Builders<UsageStats>.Update
                .Inc(x => x.TotalTokensUsed, tokensUsed)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow)
                .SetOnInsert(x => x.UserId, userId)
                .SetOnInsert(x => x.Date, today)
                .SetOnInsert(x => x.CreatedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }

        public async Task<List<UsageStats>> GetStatsForPeriodAsync(
            long userId,
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default)
        {
            var fromDateStr = fromDate.ToString("yyyy-MM-dd");
            var toDateStr = toDate.ToString("yyyy-MM-dd");

            return await _collection
                .Find(x => x.UserId == userId &&
                           x.Date.CompareTo(fromDateStr) >= 0 &&
                           x.Date.CompareTo(toDateStr) <= 0)
                .SortBy(x => x.Date)
                .ToListAsync(cancellationToken);
        }
    }
}

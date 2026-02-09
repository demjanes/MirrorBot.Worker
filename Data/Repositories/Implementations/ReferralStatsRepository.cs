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
    /// Репозиторий статистики реферальной программы.
    /// </summary>
    public class ReferralStatsRepository : BaseRepository<ReferralStats>, IReferralStatsRepository
    {
        private const string CollectionName = "referral_stats";

        public ReferralStatsRepository(IMongoDatabase database)
            : base(database, CollectionName)
        {
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            // Уникальный индекс по ownerTgUserId
            var keyIndex = Builders<ReferralStats>.IndexKeys
                .Ascending(x => x.OwnerTgUserId);

            var keyOptions = new CreateIndexOptions
            {
                Name = "ux_ownerTgUserId",
                Unique = true
            };

            _collection.Indexes.CreateOne(
                new CreateIndexModel<ReferralStats>(keyIndex, keyOptions));
        }

        public async Task<ReferralStats?> GetByOwnerTgUserIdAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<ReferralStats>.Filter
                .Eq(x => x.OwnerTgUserId, ownerTgUserId);

            return await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<ReferralStats> GetOrCreateAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default)
        {
            var existing = await GetByOwnerTgUserIdAsync(ownerTgUserId, cancellationToken);

            if (existing != null)
                return existing;

            var newStats = new ReferralStats
            {
                OwnerTgUserId = ownerTgUserId,
                TotalReferrals = 0,
                PaidReferrals = 0,
                TotalReferralRevenue = 0,
                Balance = 0,
                Currency = "RUB",
                CreatedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            };

            await _collection.InsertOneAsync(newStats, cancellationToken: cancellationToken);
            return newStats;
        }

        public async Task IncrementTotalReferralsAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<ReferralStats>.Filter
                .Eq(x => x.OwnerTgUserId, ownerTgUserId);

            var update = Builders<ReferralStats>.Update
                .Inc(x => x.TotalReferrals, 1)
                .Set(x => x.LastUpdatedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }

        public async Task IncrementPaidReferralsAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<ReferralStats>.Filter
                .Eq(x => x.OwnerTgUserId, ownerTgUserId);

            var update = Builders<ReferralStats>.Update
                .Inc(x => x.PaidReferrals, 1)
                .Set(x => x.LastUpdatedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                filter,
                update,
                cancellationToken: cancellationToken);
        }

        public async Task AddEarningsAsync(
            long ownerTgUserId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<ReferralStats>.Filter
                .Eq(x => x.OwnerTgUserId, ownerTgUserId);

            var update = Builders<ReferralStats>.Update
                .Inc(x => x.Balance, amount)
                .Inc(x => x.TotalReferralRevenue, amount)
                .Set(x => x.LastUpdatedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                filter,
                update,
                cancellationToken: cancellationToken);
        }

        public async Task DeductBalanceAsync(
            long ownerTgUserId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<ReferralStats>.Filter
                .Eq(x => x.OwnerTgUserId, ownerTgUserId);

            var update = Builders<ReferralStats>.Update
                .Inc(x => x.Balance, -amount)
                .Set(x => x.LastUpdatedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                filter,
                update,
                cancellationToken: cancellationToken);
        }
    }
}

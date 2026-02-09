using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Driver;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    /// <summary>
    /// Репозиторий транзакций реферальной программы.
    /// </summary>
    public class ReferralTransactionRepository : BaseRepository<ReferralTransaction>, IReferralTransactionRepository
    {
        private const string CollectionName = "referral_transactions";

        public ReferralTransactionRepository(IMongoDatabase database)
            : base(database, CollectionName)
        {
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            // Индекс по владельцу для быстрого получения истории
            var ownerIndex = Builders<ReferralTransaction>.IndexKeys
                .Descending(x => x.OwnerTgUserId)
                .Descending(x => x.CreatedAtUtc);

            var ownerOptions = new CreateIndexOptions
            {
                Name = "ix_ownerTgUserId_createdAt"
            };

            _collection.Indexes.CreateOne(
                new CreateIndexModel<ReferralTransaction>(ownerIndex, ownerOptions));

            // Индекс по реферу для аналитики
            var referredIndex = Builders<ReferralTransaction>.IndexKeys
                .Ascending(x => x.ReferredTgUserId);

            var referredOptions = new CreateIndexOptions
            {
                Name = "ix_referredTgUserId"
            };

            _collection.Indexes.CreateOne(
                new CreateIndexModel<ReferralTransaction>(referredIndex, referredOptions));
        }

        public async Task<List<ReferralTransaction>> GetOwnerTransactionsAsync(
            long ownerTgUserId,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<ReferralTransaction>.Filter
                .Eq(x => x.OwnerTgUserId, ownerTgUserId);

            return await _collection
                .Find(filter)
                .SortByDescending(x => x.CreatedAtUtc)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task CreateAsync(
            ReferralTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(transaction, cancellationToken: cancellationToken);
        }
    }
}

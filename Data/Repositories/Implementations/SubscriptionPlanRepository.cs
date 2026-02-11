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
    public class SubscriptionPlanRepository : BaseRepository<SubscriptionPlan>, ISubscriptionPlanRepository
    {
        public SubscriptionPlanRepository(IMongoDatabase database)
            : base(database, "subscription_plans")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var typeIndex = Builders<SubscriptionPlan>.IndexKeys
                .Ascending(x => x.Type)
                .Descending(x => x.IsActive);

            _collection.Indexes.CreateOne(
                new CreateIndexModel<SubscriptionPlan>(typeIndex));
        }

        public async Task<SubscriptionPlan?> GetByTypeAsync(
            SubscriptionType type,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.Type == type && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<SubscriptionPlan>> GetActiveRatesAsync(
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.IsActive)
                .SortBy(x => x.PriceRub)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SubscriptionPlan>> GetPremiumPlansAsync(
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.IsActive && x.Type != SubscriptionType.Free)
                .SortBy(x => x.DurationDays)
                .ToListAsync(cancellationToken);
        }
    }
}

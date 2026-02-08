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
    public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(IMongoDatabase database)
            : base(database, "subscriptions")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexKeys = Builders<Subscription>.IndexKeys
                .Ascending(x => x.UserId)
                .Descending(x => x.IsActive);
            var indexModel = new CreateIndexModel<Subscription>(indexKeys);
            _collection.Indexes.CreateOne(indexModel);
        }

        public async Task<Subscription?> GetActiveSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.UserId == userId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Subscription> CreateFreeSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = new Subscription
            {
                UserId = userId,
                Type = SubscriptionType.Free,
                StartDateUtc = DateTime.UtcNow,
                MessagesLimit = 5,
                MessagesUsed = 0,
                ResetDateUtc = DateTime.UtcNow.AddDays(1),
                IsActive = true
            };

            return await CreateAsync(subscription, cancellationToken);
        }

        public async Task<bool> CanSendMessageAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetActiveSubscriptionAsync(userId, cancellationToken);

            if (subscription == null)
                return false;

            // Premium подписки - безлимит
            if (subscription.Type != SubscriptionType.Free)
                return true;

            // Проверяем, не истек ли период сброса для Free
            if (subscription.ResetDateUtc.HasValue &&
                subscription.ResetDateUtc.Value < DateTime.UtcNow)
            {
                await ResetMessagesLimitAsync(userId, cancellationToken);
                return true;
            }

            return subscription.MessagesUsed < subscription.MessagesLimit;
        }

        public async Task<bool> UseMessageAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetActiveSubscriptionAsync(userId, cancellationToken);

            if (subscription == null)
                return false;

            // Premium подписки - не считаем
            if (subscription.Type != SubscriptionType.Free)
                return true;

            var update = Builders<Subscription>.Update
                .Inc(x => x.MessagesUsed, 1);

            var result = await _collection.UpdateOneAsync(
                x => x.Id == subscription.Id,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpgradeSubscriptionAsync(
            long userId,
            SubscriptionType type,
            string paymentId,
            CancellationToken cancellationToken = default)
        {
            // Деактивируем старую подписку
            await DeactivateSubscriptionAsync(userId, cancellationToken);

            // Создаем новую
            var duration = type switch
            {
                SubscriptionType.Monthly => 30,
                SubscriptionType.Quarterly => 90,
                SubscriptionType.Yearly => 365,
                _ => 30
            };

            var subscription = new Subscription
            {
                UserId = userId,
                Type = type,
                StartDateUtc = DateTime.UtcNow,
                EndDateUtc = DateTime.UtcNow.AddDays(duration),
                MessagesLimit = int.MaxValue,
                MessagesUsed = 0,
                IsActive = true,
                PaymentId = paymentId
            };

            await CreateAsync(subscription, cancellationToken);
            return true;
        }

        public async Task<bool> DeactivateSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Subscription>.Update
                .Set(x => x.IsActive, false);

            var result = await _collection.UpdateManyAsync(
                x => x.UserId == userId && x.IsActive,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> ResetMessagesLimitAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Subscription>.Update
                .Set(x => x.MessagesUsed, 0)
                .Set(x => x.ResetDateUtc, DateTime.UtcNow.AddDays(1));

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId && x.IsActive && x.Type == SubscriptionType.Free,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<int> GetUsedMessagesCountAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetActiveSubscriptionAsync(userId, cancellationToken);
            return subscription?.MessagesUsed ?? 0;
        }
    }
}

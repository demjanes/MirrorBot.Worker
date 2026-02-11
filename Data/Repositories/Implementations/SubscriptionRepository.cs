using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Models.Subscription;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
    {
        private readonly ILogger<SubscriptionRepository> _logger;

        public SubscriptionRepository(
            IMongoDatabase database,
            ILogger<SubscriptionRepository> logger)
            : base(database, "subscriptions")
        {
            _logger = logger;
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            // Индекс по userId и статусу активности
            var userActiveIndex = Builders<Subscription>.IndexKeys
                .Ascending(x => x.UserId)
                .Descending(x => x.IsActive);

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Subscription>(userActiveIndex));

            // Индекс для поиска истекших подписок
            var expirationIndex = Builders<Subscription>.IndexKeys
                .Ascending(x => x.EndDateUtc)
                .Ascending(x => x.Status);

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Subscription>(expirationIndex));
        }

        public async Task<Subscription?> GetActiveSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.UserId == userId && x.IsActive && x.Status == SubscriptionStatus.Active)
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
                EndDateUtc = null, // Free = бессрочная
                MessagesLimit = 10, // 10 сообщений в день для Free
                MessagesUsed = 0,
                ResetDateUtc = DateTime.UtcNow.AddDays(1),
                IsActive = true,
                Status = SubscriptionStatus.Active
            };

            return await CreateAsync(subscription, cancellationToken);
        }

        public async Task<bool> CanSendMessageAsync(
            long userId,
            bool isVoice = false,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetActiveSubscriptionAsync(userId, cancellationToken);

            if (subscription == null)
            {
                _logger.LogWarning("No active subscription found for user {UserId}", userId);
                return false;
            }

            // Проверяем истечение срока Premium подписки
            if (subscription.Type != SubscriptionType.Free &&
                subscription.EndDateUtc.HasValue &&
                subscription.EndDateUtc.Value < DateTime.UtcNow)
            {
                _logger.LogInformation(
                    "Premium subscription expired for user {UserId}",
                    userId);

                await ExpireSubscriptionAsync(subscription.Id, cancellationToken);
                return false;
            }

            // Premium подписки - безлимит
            if (subscription.Type != SubscriptionType.Free)
                return true;

            // Free подписка: проверяем сброс счетчика
            if (subscription.ResetDateUtc.HasValue &&
                subscription.ResetDateUtc.Value < DateTime.UtcNow)
            {
                await ResetMessagesLimitAsync(userId, cancellationToken);
                return true;
            }

            // Free подписка: голосовые сообщения не доступны
            if (isVoice)
            {
                _logger.LogDebug(
                    "Voice messages not available for Free tier (user {UserId})",
                    userId);
                return false;
            }

            // Free подписка: проверяем лимит
            return subscription.MessagesUsed < subscription.MessagesLimit;
        }

        public async Task<bool> UseMessageAsync(
            long userId,
            bool isVoice = false,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetActiveSubscriptionAsync(userId, cancellationToken);

            if (subscription == null)
                return false;

            // Premium подписки - не считаем лимит
            if (subscription.Type != SubscriptionType.Free)
                return true;

            // Free подписка: голосовые не поддерживаются
            if (isVoice)
                return false;

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
            ObjectId planId,
            string paymentId,
            CancellationToken cancellationToken = default)
        {
            // Проверяем, что это Premium тип
            if (type == SubscriptionType.Free)
            {
                _logger.LogWarning(
                    "Attempt to upgrade to Free tier for user {UserId}",
                    userId);
                return false;
            }

            // Деактивируем старую подписку
            await DeactivateSubscriptionAsync(userId, cancellationToken);

            // Определяем длительность
            var durationDays = type switch
            {
                SubscriptionType.PremiumMonthly => 30,
                SubscriptionType.PremiumQuarterly => 90,
                SubscriptionType.PremiumHalfYear => 180,
                SubscriptionType.PremiumYearly => 365,
                _ => 30
            };

            var subscription = new Subscription
            {
                UserId = userId,
                Type = type,
                PlanId = planId,
                StartDateUtc = DateTime.UtcNow,
                EndDateUtc = DateTime.UtcNow.AddDays(durationDays),
                MessagesLimit = -1, // Безлимит
                MessagesUsed = 0,
                ResetDateUtc = null,
                IsActive = true,
                Status = SubscriptionStatus.Active,
                PaymentId = paymentId,
                AutoRenew = false
            };

            await CreateAsync(subscription, cancellationToken);

            _logger.LogInformation(
                "User {UserId} upgraded to {Type} until {EndDate}",
                userId,
                type,
                subscription.EndDateUtc);

            return true;
        }

        public async Task<bool> DeactivateSubscriptionAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Subscription>.Update
                .Set(x => x.IsActive, false)
                .Set(x => x.Status, SubscriptionStatus.Canceled);

            var result = await _collection.UpdateManyAsync(
                x => x.UserId == userId && x.IsActive,
                update,
                cancellationToken: cancellationToken);

            if (result.ModifiedCount > 0)
            {
                _logger.LogInformation(
                    "Deactivated {Count} subscription(s) for user {UserId}",
                    result.ModifiedCount,
                    userId);
            }

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
                x => x.UserId == userId &&
                     x.IsActive &&
                     x.Type == SubscriptionType.Free,
                update,
                cancellationToken: cancellationToken);

            if (result.ModifiedCount > 0)
            {
                _logger.LogDebug(
                    "Reset message limit for user {UserId}",
                    userId);
            }

            return result.ModifiedCount > 0;
        }

        public async Task<(int TextMessages, int VoiceMessages)> GetUsedMessagesCountAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var subscription = await GetActiveSubscriptionAsync(userId, cancellationToken);

            if (subscription == null)
                return (0, 0);

            // Для Premium возвращаем 0 (безлимит)
            if (subscription.Type != SubscriptionType.Free)
                return (0, 0);

            // Для Free - текстовые из счетчика, голосовые = 0 (не доступны)
            return (subscription.MessagesUsed, 0);
        }

        public async Task<int> ExpireSubscriptionsAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var filter = Builders<Subscription>.Filter.And(
                Builders<Subscription>.Filter.Ne(x => x.Type, SubscriptionType.Free),
                Builders<Subscription>.Filter.Eq(x => x.Status, SubscriptionStatus.Active),
                Builders<Subscription>.Filter.Lt(x => x.EndDateUtc, now)
            );

            var update = Builders<Subscription>.Update
                .Set(x => x.Status, SubscriptionStatus.Expired)
                .Set(x => x.IsActive, false);

            var result = await _collection.UpdateManyAsync(
                filter,
                update,
                cancellationToken: cancellationToken);

            if (result.ModifiedCount > 0)
            {
                _logger.LogInformation(
                    "Expired {Count} subscription(s)",
                    result.ModifiedCount);
            }

            return (int)result.ModifiedCount;
        }

        private async Task ExpireSubscriptionAsync(
            ObjectId subscriptionId,
            CancellationToken cancellationToken)
        {
            var update = Builders<Subscription>.Update
                .Set(x => x.Status, SubscriptionStatus.Expired)
                .Set(x => x.IsActive, false);

            await _collection.UpdateOneAsync(
                x => x.Id == subscriptionId,
                update,
                cancellationToken: cancellationToken);
        }
    }
}

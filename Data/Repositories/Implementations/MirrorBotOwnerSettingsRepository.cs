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
    /// Репозиторий настроек владельцев зеркал.
    /// </summary>
    public class MirrorBotOwnerSettingsRepository : BaseRepository<MirrorBotOwnerSettings>, IMirrorBotOwnerSettingsRepository
    {
        private const string CollectionName = "mirror_bot_owner_settings";

        public MirrorBotOwnerSettingsRepository(IMongoDatabase database)
            : base(database, CollectionName)
        {
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            var keyIndex = Builders<MirrorBotOwnerSettings>.IndexKeys
                .Ascending(x => x.OwnerTgUserId);

            var keyOptions = new CreateIndexOptions
            {
                Name = "ux_ownerTgUserId",
                Unique = true
            };

            _collection.Indexes.CreateOne(
                new CreateIndexModel<MirrorBotOwnerSettings>(keyIndex, keyOptions));
        }

        public async Task<MirrorBotOwnerSettings?> GetByOwnerTgUserIdAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<MirrorBotOwnerSettings>.Filter
                .Eq(x => x.OwnerTgUserId, ownerTgUserId);

            return await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<MirrorBotOwnerSettings> GetOrCreateAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default)
        {
            var existing = await GetByOwnerTgUserIdAsync(ownerTgUserId, cancellationToken);

            if (existing != null)
                return existing;

            var newSettings = new MirrorBotOwnerSettings
            {
                OwnerTgUserId = ownerTgUserId,
                NotifyOnNewReferral = true,
                NotifyOnReferralEarnings = true,
                NotifyOnPayout = true,
                NotificationChatId = ownerTgUserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _collection.InsertOneAsync(newSettings, cancellationToken: cancellationToken);
            return newSettings;
        }

        public async Task UpdateNotificationSettingsAsync(
            long ownerTgUserId,
            bool? notifyOnNewReferral,
            bool? notifyOnReferralEarnings,
            bool? notifyOnPayout,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<MirrorBotOwnerSettings>.Filter
                .Eq(x => x.OwnerTgUserId, ownerTgUserId);

            var updateBuilder = Builders<MirrorBotOwnerSettings>.Update;
            var updates = new List<UpdateDefinition<MirrorBotOwnerSettings>>();

            if (notifyOnNewReferral.HasValue)
                updates.Add(updateBuilder.Set(x => x.NotifyOnNewReferral, notifyOnNewReferral.Value));

            if (notifyOnReferralEarnings.HasValue)
                updates.Add(updateBuilder.Set(x => x.NotifyOnReferralEarnings, notifyOnReferralEarnings.Value));

            if (notifyOnPayout.HasValue)
                updates.Add(updateBuilder.Set(x => x.NotifyOnPayout, notifyOnPayout.Value));

            if (updates.Count > 0)
            {
                var combined = updateBuilder.Combine(updates);
                await _collection.UpdateOneAsync(
                    filter,
                    combined,
                    new UpdateOptions { IsUpsert = true },
                    cancellationToken);
            }
        }
    }
}

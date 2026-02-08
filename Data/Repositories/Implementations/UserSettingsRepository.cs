using MirrorBot.Worker.Data.Models.English;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    public class UserSettingsRepository : BaseRepository<UserSettings>, IUserSettingsRepository
    {
        public UserSettingsRepository(IMongoDatabase database)
            : base(database, "user_settings")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexKeys = Builders<UserSettings>.IndexKeys.Ascending(x => x.UserId);
            var indexModel = new CreateIndexModel<UserSettings>(indexKeys,
                new CreateIndexOptions { Unique = true });
            _collection.Indexes.CreateOne(indexModel);
        }

        public async Task<UserSettings?> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<UserSettings> CreateDefaultAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var settings = new UserSettings
            {
                UserId = userId,
                PreferredVoice = "jane",
                SpeechSpeed = 1.0,
                AutoVoiceResponse = true,
                ShowCorrections = true,
                CorrectionLevel = "Detailed",
                AutoAddToVocabulary = true,
                DefaultMode = "Casual",
                DailyReminders = false
            };

            return await CreateAsync(settings, cancellationToken);
        }

        public async Task<bool> UpdateVoiceAsync(
            long userId,
            string voice,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<UserSettings>.Update
                .Set(x => x.PreferredVoice, voice);

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateSpeechSpeedAsync(
            long userId,
            double speed,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<UserSettings>.Update
                .Set(x => x.SpeechSpeed, speed);

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> ToggleAutoVoiceResponseAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var settings = await GetByUserIdAsync(userId, cancellationToken);
            if (settings == null) return false;

            var update = Builders<UserSettings>.Update
                .Set(x => x.AutoVoiceResponse, !settings.AutoVoiceResponse);

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateDefaultModeAsync(
            long userId,
            string mode,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<UserSettings>.Update
                .Set(x => x.DefaultMode, mode);

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }
    }
}

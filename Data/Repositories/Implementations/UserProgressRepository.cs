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
    public class UserProgressRepository : BaseRepository<UserProgress>, IUserProgressRepository
    {
        public UserProgressRepository(IMongoDatabase database)
            : base(database, "user_progress")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexKeys = Builders<UserProgress>.IndexKeys.Ascending(x => x.UserId);
            var indexModel = new CreateIndexModel<UserProgress>(indexKeys,
                new CreateIndexOptions { Unique = true });
            _collection.Indexes.CreateOne(indexModel);
        }

        public async Task<UserProgress?> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> IncrementMessagesAsync(
            long userId,
            bool isVoice,
            CancellationToken cancellationToken = default)
        {
            var updateBuilder = Builders<UserProgress>.Update
                .Inc(x => x.TotalMessages, 1)
                .Set(x => x.LastActivityUtc, DateTime.UtcNow);

            if (isVoice)
                updateBuilder = updateBuilder.Inc(x => x.VoiceMessages, 1);

            var options = new UpdateOptions { IsUpsert = true };
            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                updateBuilder,
                options,
                cancellationToken);

            return result.ModifiedCount > 0 || result.UpsertedId != null;
        }

        public async Task<bool> AddCorrectionsAsync(
            long userId,
            int count,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<UserProgress>.Update
                .Inc(x => x.TotalCorrections, count);

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateVocabularySizeAsync(
            long userId,
            int size,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<UserProgress>.Update
                .Set(x => x.VocabularySize, size);

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdatePronunciationScoreAsync(
            long userId,
            int score,
            CancellationToken cancellationToken = default)
        {
            var progress = await GetByUserIdAsync(userId, cancellationToken);
            if (progress == null) return false;

            // Вычисляем новый средний балл
            var totalVoice = progress.VoiceMessages;
            var currentAvg = progress.AvgPronunciationScore;
            var newAvg = ((currentAvg * totalVoice) + score) / (totalVoice + 1);

            var update = Builders<UserProgress>.Update
                .Set(x => x.AvgPronunciationScore, newAvg);

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateStreakAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var progress = await GetByUserIdAsync(userId, cancellationToken);
            if (progress == null) return false;

            var now = DateTime.UtcNow;
            var daysSinceLastActivity = (now - progress.LastActivityUtc).TotalDays;

            int newStreak;
            if (daysSinceLastActivity <= 1)
            {
                // Продолжаем streak
                newStreak = progress.CurrentStreak + 1;
            }
            else
            {
                // Сброс streak
                newStreak = 1;
            }

            var update = Builders<UserProgress>.Update
                .Set(x => x.CurrentStreak, newStreak)
                .Set(x => x.BestStreak, Math.Max(newStreak, progress.BestStreak));

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateLevelAsync(
            long userId,
            string level,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<UserProgress>.Update
                .Set(x => x.CurrentLevel, level);

            var result = await _collection.UpdateOneAsync(
                x => x.UserId == userId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }
    }
}

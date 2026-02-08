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
    public class VocabularyRepository : BaseRepository<UserVocabulary>, IVocabularyRepository
    {
        public VocabularyRepository(IMongoDatabase database)
            : base(database, "vocabularies")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexKeys = Builders<UserVocabulary>.IndexKeys.Ascending(x => x.UserId);
            var indexModel = new CreateIndexModel<UserVocabulary>(indexKeys,
                new CreateIndexOptions { Unique = true });
            _collection.Indexes.CreateOne(indexModel);
        }

        public async Task<UserVocabulary?> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> AddWordAsync(
            long userId,
            VocabularyWord word,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserVocabulary>.Filter.Eq(x => x.UserId, userId);
            var update = Builders<UserVocabulary>.Update
                .AddToSet(x => x.Words, word)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            var options = new UpdateOptions { IsUpsert = true };
            var result = await _collection.UpdateOneAsync(filter, update, options, cancellationToken);

            return result.ModifiedCount > 0 || result.UpsertedId != null;
        }

        public async Task<bool> RemoveWordAsync(
            long userId,
            string word,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserVocabulary>.Filter.Eq(x => x.UserId, userId);
            var update = Builders<UserVocabulary>.Update
                .PullFilter(x => x.Words, w => w.Word == word)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateWordReviewAsync(
            long userId,
            string word,
            CancellationToken cancellationToken = default)
        {
            var vocabulary = await GetByUserIdAsync(userId, cancellationToken);
            if (vocabulary == null) return false;

            var wordToUpdate = vocabulary.Words.FirstOrDefault(w => w.Word == word);
            if (wordToUpdate == null) return false;

            wordToUpdate.ReviewCount++;
            wordToUpdate.LastReviewUtc = DateTime.UtcNow;

            if (wordToUpdate.ReviewCount >= 5)
                wordToUpdate.IsLearned = true;

            vocabulary.UpdatedAtUtc = DateTime.UtcNow;

            return await UpdateAsync(vocabulary, cancellationToken);
        }

        public async Task<List<VocabularyWord>> GetWordsForReviewAsync(
            long userId,
            int count,
            CancellationToken cancellationToken = default)
        {
            var vocabulary = await GetByUserIdAsync(userId, cancellationToken);
            if (vocabulary == null) return new List<VocabularyWord>();

            var now = DateTime.UtcNow;
            var oneDayAgo = now.AddDays(-1);

            return vocabulary.Words
                .Where(w => !w.IsLearned &&
                           (w.LastReviewUtc == null || w.LastReviewUtc < oneDayAgo))
                .OrderBy(w => w.LastReviewUtc ?? DateTime.MinValue)
                .Take(count)
                .ToList();
        }

        public async Task<VocabularyWord?> FindWordAsync(
            long userId,
            string word,
            CancellationToken cancellationToken = default)
        {
            var vocabulary = await GetByUserIdAsync(userId, cancellationToken);
            return vocabulary?.Words.FirstOrDefault(w =>
                w.Word.Equals(word, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<int> GetVocabularySizeAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var vocabulary = await GetByUserIdAsync(userId, cancellationToken);
            return vocabulary?.Words.Count ?? 0;
        }
    }
}

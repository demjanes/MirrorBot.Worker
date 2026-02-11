using MirrorBot.Worker.Data.Models.English;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
    {
        private const string CollectionName = "conversations";

        public ConversationRepository(IMongoDatabase database)
            : base(database, CollectionName)
        {
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            // Уникальный индекс по userId
            var userIdIndex = Builders<Conversation>.IndexKeys
                .Ascending(x => x.UserId);

            var userIdOptions = new CreateIndexOptions
            {
                Name = "ux_userId",
                Unique = true
            };

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Conversation>(userIdIndex, userIdOptions));

            // Индекс по активности
            var activityIndex = Builders<Conversation>.IndexKeys
                .Descending(x => x.LastActivityUtc);

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Conversation>(activityIndex));
        }

        public async Task<Conversation?> GetByUserAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<Conversation>.Filter.Eq(x => x.UserId, userId);

            return await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Conversation> CreateOrUpdateAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<Conversation>.Filter.Eq(x => x.UserId, conversation.UserId);

            var options = new FindOneAndReplaceOptions<Conversation>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var result = await _collection.FindOneAndReplaceAsync(
                filter,
                conversation,
                options,
                cancellationToken);

            return result;
        }
    }
}

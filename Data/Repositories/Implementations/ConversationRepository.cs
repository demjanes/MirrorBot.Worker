using MirrorBot.Worker.Data.Models.English;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(IMongoDatabase database)
            : base(database, "conversations")
        {
            // Создаем индексы для быстрого поиска
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexKeys = Builders<Conversation>.IndexKeys
                .Ascending(x => x.UserId)
                .Ascending(x => x.BotId)
                .Descending(x => x.LastActivityUtc);

            var indexModel = new CreateIndexModel<Conversation>(indexKeys);
            _collection.Indexes.CreateOne(indexModel);
        }

        public async Task<Conversation?> GetActiveConversationAsync(
            long userId,
            string botId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.UserId == userId && x.BotId == botId && x.IsActive)
                .SortByDescending(x => x.LastActivityUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.LastActivityUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> AddMessageAsync(
            ObjectId conversationId,
            EnglishMessage message,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Conversation>.Update
                .Push(x => x.Messages, message)
                .Set(x => x.LastActivityUtc, DateTime.UtcNow)
                .Inc(x => x.TotalTokensUsed, message.TokensUsed);

            var result = await _collection.UpdateOneAsync(
                x => x.Id == conversationId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateModeAsync(
            ObjectId conversationId,
            string mode,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Conversation>.Update
                .Set(x => x.Mode, mode)
                .Set(x => x.LastActivityUtc, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(
                x => x.Id == conversationId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> CloseConversationAsync(
            ObjectId conversationId,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Conversation>.Update
                .Set(x => x.IsActive, false);

            var result = await _collection.UpdateOneAsync(
                x => x.Id == conversationId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }

        public async Task<List<EnglishMessage>> GetRecentMessagesAsync(
            ObjectId conversationId,
            int count,
            CancellationToken cancellationToken = default)
        {
            var conversation = await GetByIdAsync(conversationId, cancellationToken);

            if (conversation == null)
                return new List<EnglishMessage>();

            return conversation.Messages
                .OrderByDescending(x => x.TimestampUtc)
                .Take(count)
                .OrderBy(x => x.TimestampUtc)
                .ToList();
        }

        public async Task<bool> UpdateLastActivityAsync(
            ObjectId conversationId,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Conversation>.Update
                .Set(x => x.LastActivityUtc, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(
                x => x.Id == conversationId,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount > 0;
        }
    }
}

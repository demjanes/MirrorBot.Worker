using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Implementations
{
    /// <summary>
    /// Базовая реализация репозитория для MongoDB
    /// </summary>
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : BaseEntity
    {
        protected readonly IMongoCollection<TEntity> _collection;

        protected BaseRepository(IMongoDatabase database, string collectionName)
        {
            _collection = database.GetCollection<TEntity>(collectionName);
        }

        public virtual async Task<TEntity?> GetByIdAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<List<TEntity>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(_ => true)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<List<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(filter)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<TEntity?> FindOneAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<TEntity> CreateAsync(
            TEntity entity,
            CancellationToken cancellationToken = default)
        {
            entity.CreatedAtUtc = DateTime.UtcNow;
            await _collection.InsertOneAsync(entity, null, cancellationToken);
            return entity;
        }

        public virtual async Task<bool> UpdateAsync(
            TEntity entity,
            CancellationToken cancellationToken = default)
        {
            var result = await _collection
                .ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }

        public virtual async Task<bool> DeleteAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            var result = await _collection
                .DeleteOneAsync(x => x.Id == id, cancellationToken);
            return result.DeletedCount > 0;
        }

        public virtual async Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(filter)
                .AnyAsync(cancellationToken);
        }

        public virtual async Task<long> CountAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        }
    }
}

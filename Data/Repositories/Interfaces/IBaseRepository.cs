using MirrorBot.Worker.Data.Models.Core;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Repositories.Interfaces
{
    /// <summary>
    /// Базовый интерфейс для всех репозиториев
    /// </summary>
    public interface IBaseRepository<TEntity> where TEntity : BaseEntity
    {
        /// <summary>
        /// Получить по ID
        /// </summary>
        Task<TEntity?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить все записи
        /// </summary>
        Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Найти по условию
        /// </summary>
        Task<List<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Найти одну запись по условию
        /// </summary>
        Task<TEntity?> FindOneAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создать новую запись
        /// </summary>
        Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить запись
        /// </summary>
        Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Удалить по ID
        /// </summary>
        Task<bool> DeleteAsync(ObjectId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверить существование по условию
        /// </summary>
        Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Подсчитать количество по условию
        /// </summary>
        Task<long> CountAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default);
    }
}

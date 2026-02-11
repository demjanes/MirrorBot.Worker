using MirrorBot.Worker.Data.Models.Payments;
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
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(IMongoDatabase database)
            : base(database, "payments")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            // Индекс по userId
            var userIndex = Builders<Payment>.IndexKeys
                .Descending(x => x.UserId)
                .Descending(x => x.CreatedAtUtc);

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Payment>(userIndex));

            // Уникальный индекс по externalPaymentId
            var externalIdIndex = Builders<Payment>.IndexKeys
                .Ascending(x => x.ExternalPaymentId);

            var uniqueOptions = new CreateIndexOptions
            {
                Unique = true,
                Name = "ux_external_payment_id"
            };

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Payment>(externalIdIndex, uniqueOptions));

            // Индекс по статусу и дате
            var statusIndex = Builders<Payment>.IndexKeys
                .Ascending(x => x.Status)
                .Descending(x => x.CreatedAtUtc);

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Payment>(statusIndex));

            // Индекс по провайдеру
            var providerIndex = Builders<Payment>.IndexKeys
                .Ascending(x => x.Provider)
                .Descending(x => x.CreatedAtUtc);

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Payment>(providerIndex));
        }

        public async Task<Payment?> GetByExternalIdAsync(
            string externalPaymentId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.ExternalPaymentId == externalPaymentId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Payment>> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.UserId == userId)
                .SortByDescending(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<Payment?> UpdateExternalDataAsync(
            ObjectId paymentId,
            string externalPaymentId,
            string? paymentUrl,
            string? providerData,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Payment>.Update
                .Set(x => x.ExternalPaymentId, externalPaymentId)
                .Set(x => x.PaymentUrl, paymentUrl)
                .Set(x => x.ProviderData, providerData)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            return await _collection.FindOneAndUpdateAsync(
                x => x.Id == paymentId,
                update,
                new FindOneAndUpdateOptions<Payment> { ReturnDocument = ReturnDocument.After },
                cancellationToken);
        }

        public async Task<Payment?> UpdateStatusAsync(
            ObjectId paymentId,
            PaymentStatus status,
            DateTime? paidAtUtc = null,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Payment>.Update
                .Set(x => x.Status, status)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            if (paidAtUtc.HasValue)
            {
                update = update.Set(x => x.PaidAtUtc, paidAtUtc.Value);
            }

            return await _collection.FindOneAndUpdateAsync(
                x => x.Id == paymentId,
                update,
                new FindOneAndUpdateOptions<Payment> { ReturnDocument = ReturnDocument.After },
                cancellationToken);
        }

        public async Task MarkReferralRewardProcessedAsync(
            ObjectId paymentId,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Payment>.Update
                .Set(x => x.ReferralRewardProcessed, true)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(
                x => x.Id == paymentId,
                update,
                cancellationToken: cancellationToken);
        }
    }
}

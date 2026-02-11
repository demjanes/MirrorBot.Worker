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

            // Уникальный индекс по yookassaPaymentId
            var yookassaIndex = Builders<Payment>.IndexKeys
                .Ascending(x => x.YookassaPaymentId);

            var uniqueOptions = new CreateIndexOptions
            {
                Unique = true,
                Name = "ux_yookassa_payment_id"
            };

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Payment>(yookassaIndex, uniqueOptions));

            // Индекс по статусу и дате
            var statusIndex = Builders<Payment>.IndexKeys
                .Ascending(x => x.Status)
                .Descending(x => x.CreatedAtUtc);

            _collection.Indexes.CreateOne(
                new CreateIndexModel<Payment>(statusIndex));
        }

        public async Task<Payment?> GetByYookassaIdAsync(
            string yookassaPaymentId,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.YookassaPaymentId == yookassaPaymentId)
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

        public async Task<Payment?> UpdateYookassaDataAsync(
            ObjectId paymentId,
            string yookassaPaymentId,
            string? confirmationUrl,
            string? metadata,
            CancellationToken cancellationToken = default)
        {
            var update = Builders<Payment>.Update
                .Set(x => x.YookassaPaymentId, yookassaPaymentId)
                .Set(x => x.ConfirmationUrl, confirmationUrl)
                .Set(x => x.Metadata, metadata)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

            return await _collection.FindOneAndUpdateAsync(
                x => x.Id == paymentId,
                update,
                new FindOneAndUpdateOptions<Payment> { ReturnDocument = ReturnDocument.After },
                cancellationToken);
        }
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Referral
{
    /// <summary>
    /// Реализация сервиса реферальной программы.
    /// </summary>
    public class ReferralService : IReferralService
    {
        private readonly IUsersRepository _usersRepo;
        private readonly IReferralStatsRepository _statsRepo;
        private readonly IReferralTransactionRepository _transactionRepo;
        private readonly IMirrorBotOwnerSettingsRepository _ownerSettingsRepo;
        private readonly IReferralNotificationService _notificationService;
        private readonly IOptions<ReferralConfiguration> _config;
        private readonly ILogger<ReferralService> _logger;

        public ReferralService(
            IUsersRepository usersRepo,
            IReferralStatsRepository statsRepo,
            IReferralTransactionRepository transactionRepo,
            IMirrorBotOwnerSettingsRepository ownerSettingsRepo,
            IReferralNotificationService notificationService,
            IOptions<ReferralConfiguration> config,
            ILogger<ReferralService> logger)
        {
            _usersRepo = usersRepo;
            _statsRepo = statsRepo;
            _transactionRepo = transactionRepo;
            _ownerSettingsRepo = ownerSettingsRepo;
            _notificationService = notificationService;
            _config = config;
            _logger = logger;
        }

        public async Task RegisterReferralAsync(
     long userId,
     long? referrerOwnerTgUserId,
     ObjectId? referrerMirrorBotId,
     CancellationToken cancellationToken = default)
        {
            try
            {
                if (!referrerOwnerTgUserId.HasValue)
                    return;

                if (referrerOwnerTgUserId == userId)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to become their own referral",
                        userId);
                    return;
                }

                // ✅ УПРОЩЕНО: Если метод вызван, значит это новый пользователь с рефералом
                // Создаем или получаем статистику
                await _statsRepo.GetOrCreateAsync(referrerOwnerTgUserId.Value, cancellationToken);

                // Увеличиваем счетчик рефералов
                await _statsRepo.IncrementTotalReferralsAsync(
                    referrerOwnerTgUserId.Value,
                    cancellationToken);

                // Отправляем уведомление
                await _notificationService.NotifyNewReferralAsync(
                    referrerOwnerTgUserId.Value,
                    userId,
                    referrerMirrorBotId,
                    cancellationToken);

                _logger.LogInformation(
                    "Registered NEW referral: User {UserId} -> Owner {OwnerId}, Mirror {MirrorId}",
                    userId,
                    referrerOwnerTgUserId,
                    referrerMirrorBotId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error registering referral for user {UserId}",
                    userId);
            }
        }


        public async Task OnPaymentSucceededAsync(
            long payerTgUserId,
            decimal amount,
            string currency,
            string paymentId,
            string source,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Получаем пользователя и проверяем, есть ли у него реферер
                var payer = await _usersRepo.GetByTelegramIdAsync(
                    payerTgUserId,
                    cancellationToken);

                if (payer?.ReferrerOwnerTgUserId == null)
                {
                    _logger.LogDebug(
                        "Payment from user {UserId} has no referrer",
                        payerTgUserId);
                    return;
                }

                var referrerOwnerTgUserId = payer.ReferrerOwnerTgUserId.Value;
                var referrerMirrorBotId = payer.ReferrerMirrorBotId;

                // Вычисляем бонус
                var referralAmount = amount * _config.Value.ReferralPercentage;

                // Проверяем, первая ли это оплата от этого реферала
                var existingTransactions = await _transactionRepo.GetOwnerTransactionsAsync(
                    referrerOwnerTgUserId,
                    limit: 1000,
                    cancellationToken);

                var isFirstPayment = !existingTransactions.Exists(
                    t => t.ReferredTgUserId == payerTgUserId &&
                         t.Kind == ReferralTransactionKind.Accrual);

                // Если первая оплата - инкрементируем счетчик платящих рефералов
                if (isFirstPayment)
                {
                    await _statsRepo.IncrementPaidReferralsAsync(
                        referrerOwnerTgUserId,
                        cancellationToken);
                }

                // Создаём транзакцию
                var transaction = new ReferralTransaction
                {
                    OwnerTgUserId = referrerOwnerTgUserId,
                    ReferredTgUserId = payerTgUserId,
                    MirrorBotId = referrerMirrorBotId,
                    Amount = referralAmount,
                    Currency = currency,
                    Kind = ReferralTransactionKind.Accrual,
                    PaymentId = paymentId,
                    Source = source,
                    Description = $"Referral bonus from user {payerTgUserId}",
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _transactionRepo.CreateAsync(transaction, cancellationToken);

                // Обновляем баланс и доход
                await _statsRepo.AddEarningsAsync(
                    referrerOwnerTgUserId,
                    referralAmount,
                    cancellationToken);

                // Уведомляем владельца о начислении
                await _notificationService.NotifyReferralEarningAsync(
                    referrerOwnerTgUserId,
                    payerTgUserId,
                    referralAmount,
                    currency,
                    cancellationToken);

                _logger.LogInformation(
                    "Accrued referral bonus: Owner {OwnerId} earned {Amount} {Currency} from user {PayerId}",
                    referrerOwnerTgUserId,
                    referralAmount,
                    currency,
                    payerTgUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing referral payment for user {UserId}",
                    payerTgUserId);
            }
        }

        public async Task<ReferralStats?> GetOwnerStatsAsync(
            long ownerTgUserId,
            CancellationToken cancellationToken = default)
        {
            return await _statsRepo.GetByOwnerTgUserIdAsync(
                ownerTgUserId,
                cancellationToken);
        }

        public async Task<List<ReferralTransaction>> GetOwnerTransactionsAsync(
            long ownerTgUserId,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            return await _transactionRepo.GetOwnerTransactionsAsync(
                ownerTgUserId,
                limit,
                cancellationToken);
        }

        public async Task<(bool Success, string? ErrorMessage)> RequestPayoutAsync(
            long ownerTgUserId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _statsRepo.GetByOwnerTgUserIdAsync(
                    ownerTgUserId,
                    cancellationToken);

                if (stats == null)
                {
                    return (false, "Статистика не найдена");
                }

                // Проверяем минимальную сумму
                if (amount < _config.Value.MinimumPayoutAmount)
                {
                    return (false, $"Минимальная сумма для вывода: {_config.Value.MinimumPayoutAmount} {stats.Currency}");
                }

                // Проверяем достаточность баланса
                if (stats.Balance < amount)
                {
                    return (false, $"Недостаточно средств. Доступно: {stats.Balance} {stats.Currency}");
                }

                // Создаём транзакцию на вывод
                var transaction = new ReferralTransaction
                {
                    OwnerTgUserId = ownerTgUserId,
                    Amount = -amount,
                    Currency = stats.Currency,
                    Kind = ReferralTransactionKind.Payout,
                    Description = $"Payout request: {amount} {stats.Currency}",
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _transactionRepo.CreateAsync(transaction, cancellationToken);

                // Обновляем баланс
                await _statsRepo.DeductBalanceAsync(
                    ownerTgUserId,
                    amount,
                    cancellationToken);

                // Уведомляем владельца
                await _notificationService.NotifyPayoutRequestAsync(
                    ownerTgUserId,
                    amount,
                    stats.Currency,
                    cancellationToken);

                _logger.LogInformation(
                    "Payout requested: Owner {OwnerId}, Amount {Amount} {Currency}",
                    ownerTgUserId,
                    amount,
                    stats.Currency);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing payout request for owner {OwnerId}",
                    ownerTgUserId);

                return (false, "Ошибка при обработке запроса на вывод");
            }
        }

        public async Task ProcessReferralPaymentAsync(
            long referrerId,
            long referralUserId,
            decimal paymentAmount,
            decimal rewardAmount,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Проверяем, первая ли это оплата от этого реферала
                var existingTransactions = await _transactionRepo.GetOwnerTransactionsAsync(
                    referrerId,
                    limit: 1000,
                    cancellationToken);

                var isFirstPayment = !existingTransactions.Exists(
                    t => t.ReferredTgUserId == referralUserId &&
                         t.Kind == ReferralTransactionKind.Accrual);

                // Если первая оплата - инкрементируем счетчик платящих рефералов
                if (isFirstPayment)
                {
                    await _statsRepo.IncrementPaidReferralsAsync(
                        referrerId,
                        cancellationToken);
                }

                // Создаем транзакцию с правильными полями
                var transaction = new ReferralTransaction
                {
                    OwnerTgUserId = referrerId,
                    ReferredTgUserId = referralUserId,
                    MirrorBotId = null, // Платеж через основную систему, не через зеркало
                    Amount = rewardAmount,
                    Currency = "RUB",
                    Kind = ReferralTransactionKind.Accrual,
                    PaymentId = null, // Можно передать ID платежа из Payment, если нужно
                    Source = "YooKassa",
                    Description = $"Реферальный бонус за оплату подписки на сумму {paymentAmount:F2}₽"
                };

                await _transactionRepo.CreateAsync(transaction, cancellationToken);

                // Обновляем баланс и доход
                await _statsRepo.AddEarningsAsync(referrerId, rewardAmount, cancellationToken);

                // Отправляем уведомление рефереру (правильное имя метода)
                await _notificationService.NotifyReferralEarningAsync(
                    referrerId,
                    referralUserId,
                    rewardAmount,
                    "RUB",
                    cancellationToken);

                _logger.LogInformation(
                    "Referral reward processed: ReferrerId={ReferrerId}, ReferralUserId={ReferralUserId}, " +
                    "PaymentAmount={PaymentAmount}₽, RewardAmount={RewardAmount}₽",
                    referrerId,
                    referralUserId,
                    paymentAmount,
                    rewardAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing referral payment: ReferrerId={ReferrerId}, ReferralUserId={ReferralUserId}",
                    referrerId,
                    referralUserId);
                throw;
            }
        }
    }

}

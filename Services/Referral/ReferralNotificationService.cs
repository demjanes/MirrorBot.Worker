using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MongoDB.Bson;
using Telegram.Bot;

namespace MirrorBot.Worker.Services.Referral
{
    /// <summary>
    /// Реализация сервиса уведомлений о реферальной активности.
    /// Пользователь получает уведомление в бот, с которым взаимодействовал последним.
    /// </summary>
    public class ReferralNotificationService : IReferralNotificationService
    {
        private readonly IMirrorBotOwnerSettingsRepository _ownerSettingsRepo;
        private readonly IMirrorBotsRepository _mirrorBotsRepo;
        private readonly IUsersRepository _usersRepo;
        private readonly IBotClientResolver _botClientResolver;
        private readonly ILogger<ReferralNotificationService> _logger;

        public ReferralNotificationService(
            IMirrorBotOwnerSettingsRepository ownerSettingsRepo,
            IMirrorBotsRepository mirrorBotsRepo,
            IUsersRepository usersRepo,
            IBotClientResolver botClientResolver,
            ILogger<ReferralNotificationService> logger)
        {
            _ownerSettingsRepo = ownerSettingsRepo;
            _mirrorBotsRepo = mirrorBotsRepo;
            _usersRepo = usersRepo;
            _botClientResolver = botClientResolver;
            _logger = logger;
        }

        public async Task NotifyNewReferralAsync(
            long ownerTgUserId,
            long referralTgUserId,
            ObjectId? mirrorBotId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = await _ownerSettingsRepo.GetOrCreateAsync(
                    ownerTgUserId,
                    cancellationToken);

                if (!settings.NotifyOnNewReferral)
                    return;

                // Получаем информацию о реферале
                var referralUser = await _usersRepo.GetByTelegramIdAsync(
                    referralTgUserId,
                    cancellationToken);

                var referralName = referralUser?.TgUsername ?? $"User {referralTgUserId}";

                // Формируем текст уведомления
                var message = $"🎉 <b>Новый реферал!</b>\n\n" +
                             $"К вашему боту присоединился новый пользователь:\n" +
                             $"👤 {EscapeHtml(referralName)}\n" +
                             $"🆔 ID: de>{referralTgUserId}</code>";

                if (mirrorBotId.HasValue && mirrorBotId != ObjectId.Empty)
                {
                    var mirrorBot = await _mirrorBotsRepo.GetByIdAsync(
                        mirrorBotId.Value,
                        cancellationToken);

                    if (mirrorBot != null)
                    {
                        message += $"\n🤖 Через бота: @{EscapeHtml(mirrorBot.BotUsername ?? "unknown")}";
                    }
                }

                // Получаем владельца и отправляем ему уведомление в его последний бот
                var owner = await _usersRepo.GetByTelegramIdAsync(
                    ownerTgUserId,
                    cancellationToken);

                if (owner != null)
                {
                    await SendNotificationAsync(
                        owner,
                        message,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending new referral notification to owner {OwnerId}",
                    ownerTgUserId);
            }
        }

        public async Task NotifyReferralEarningAsync(
            long ownerTgUserId,
            long referralTgUserId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = await _ownerSettingsRepo.GetOrCreateAsync(
                    ownerTgUserId,
                    cancellationToken);

                if (!settings.NotifyOnReferralEarnings)
                    return;

                // Получаем информацию о реферале
                var referralUser = await _usersRepo.GetByTelegramIdAsync(
                    referralTgUserId,
                    cancellationToken);

                var referralName = referralUser?.TgUsername ?? $"User {referralTgUserId}";

                // Форматируем сумму
                var formattedAmount = FormatAmount(amount, currency);

                // Формируем текст уведомления
                var message = $"💰 <b>Пополнение баланса!</b>\n\n" +
                             $"Вам начислен реферальный бонус: <b>{formattedAmount}</b>\n" +
                             $"От реферала: {EscapeHtml(referralName)}\n" +
                             $"🆔 ID: de>{referralTgUserId}</code>";

                // Получаем владельца и отправляем ему уведомление в его последний бот
                var owner = await _usersRepo.GetByTelegramIdAsync(
                    ownerTgUserId,
                    cancellationToken);

                if (owner != null)
                {
                    await SendNotificationAsync(
                        owner,
                        message,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending earning notification to owner {OwnerId}",
                    ownerTgUserId);
            }
        }

        public async Task NotifyPayoutRequestAsync(
            long ownerTgUserId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = await _ownerSettingsRepo.GetOrCreateAsync(
                    ownerTgUserId,
                    cancellationToken);

                if (!settings.NotifyOnPayout)
                    return;

                var formattedAmount = FormatAmount(amount, currency);

                var message = $"✅ <b>Запрос на вывод принят!</b>\n\n" +
                             $"Сумма: <b>{formattedAmount}</b>\n" +
                             $"Статус: В обработке\n\n" +
                             $"Средства будут зачислены в течение 1-3 рабочих дней.";

                // Получаем владельца и отправляем ему уведомление в его последний бот
                var owner = await _usersRepo.GetByTelegramIdAsync(
                    ownerTgUserId,
                    cancellationToken);

                if (owner != null)
                {
                    await SendNotificationAsync(
                        owner,
                        message,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending payout notification to owner {OwnerId}",
                    ownerTgUserId);
            }
        }

        private async Task SendNotificationAsync(
            User owner,
            string message,
            CancellationToken cancellationToken)
        {
            try
            {
                // Проверяем, есть ли у владельца lastBotKey
                if (string.IsNullOrEmpty(owner.LastBotKey))
                {
                    _logger.LogWarning(
                        "Owner {OwnerId} has no LastBotKey, cannot send notification",
                        owner.TgUserId);
                    return;
                }

                // Проверяем, есть ли lastChatId
                if (!owner.LastChatId.HasValue)
                {
                    _logger.LogWarning(
                        "Owner {OwnerId} has no LastChatId, cannot send notification",
                        owner.TgUserId);
                    return;
                }

                // Пытаемся получить bot client по ключу последнего использованного бота
                if (!_botClientResolver.TryGetClient(owner.LastBotKey, out var botClient))
                {
                    _logger.LogWarning(
                        "Could not resolve bot client for owner {OwnerId} with botKey {BotKey}",
                        owner.TgUserId,
                        owner.LastBotKey);
                    return;
                }

                // Проверяем, может ли владелец отправлять сообщения
                if (!owner.CanSendLastBot)
                {
                    _logger.LogInformation(
                        "Owner {OwnerId} has CanSendLastBot = false, cannot send notification",
                        owner.TgUserId);
                    return;
                }

                // Отправляем уведомление в последний бот владельца
                await botClient.SendMessage(
                    owner.LastChatId.Value,
                    message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Notification sent to owner {OwnerId} via bot {BotKey}",
                    owner.TgUserId,
                    owner.LastBotKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send notification to owner {OwnerId}",
                    owner.TgUserId);
            }
        }

        private static string FormatAmount(decimal amount, string currency)
        {
            return currency.ToUpperInvariant() switch
            {
                "RUB" => $"{amount:N2} ₽",
                "USD" => $"${amount:N2}",
                "EUR" => $"€{amount:N2}",
                _ => $"{amount:N2} {currency}"
            };
        }

        private static string EscapeHtml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}

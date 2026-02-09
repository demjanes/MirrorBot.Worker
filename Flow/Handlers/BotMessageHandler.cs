using Microsoft.Extensions.Options;
using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI;
using MirrorBot.Worker.Services;
using MirrorBot.Worker.Services.AdminNotifierService;
using MirrorBot.Worker.Services.Referral;
using MirrorBot.Worker.Services.TokenEncryption;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.Handlers
{
    public sealed class BotMessageHandler
    {
        private static readonly Regex TokenRegex =
         new(@"^[0-9]{8,10}:[a-zA-Z0-9_-]{35}$", RegexOptions.Compiled);

        private const bool AllowSecretsInAdminLogs = false;

        private readonly IUsersRepository _users;
        private readonly IMirrorBotsRepository _mirrorBots;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAdminNotifier _notifier;
        private readonly ITokenEncryptionService _tokenEncryption;
        private readonly IOptions<LimitsConfiguration> _limitsOptions;
        private readonly IReferralService _referralService; // ← НОВОЕ

        public BotMessageHandler(
     IUsersRepository users,
     IMirrorBotsRepository mirrorBots,
     IHttpClientFactory httpClientFactory,
     IAdminNotifier notifier,
     ITokenEncryptionService tokenEncryptionService,
     IOptions<LimitsConfiguration> limitsOptions,
     IReferralService referralService)
        {          
            _users = users;
            _mirrorBots = mirrorBots;
            _httpClientFactory = httpClientFactory;
            _notifier = notifier;
            _tokenEncryption = tokenEncryptionService;
            _limitsOptions = limitsOptions;
            _referralService = referralService;
        }

        public async System.Threading.Tasks.Task HandleAsync(BotContext ctx, ITelegramBotClient client, Message msg, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            if (msg?.From is null) return;

            var rawText = msg.Text;
            if (string.IsNullOrWhiteSpace(rawText)) return;

            var text = rawText.Trim();
            var chatId = msg.Chat.Id;

            var taskEntity = new Data.Models.Core.BotTask
            {
                BotContext = ctx,
                TgClient = client,
                TgChatId = chatId,
                TgMessage = msg,
                TgUserText = text,
            };

            // ============ НОВОЕ: Парсим /start параметр ============
            string? startParameter = null;
            if (text.StartsWith(BotRoutes.Commands.Start, StringComparison.OrdinalIgnoreCase))
            {
                // Извлекаем параметр после "/start "
                var parts = text.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    startParameter = parts[1];
                }
            }
            // ========================================================

            await UpsertSeenAsync(taskEntity, startParameter, ct); // ← ИЗМЕНЕНО: передаем startParameter

            switch (text.Split(' ')[0]) // ← ИЗМЕНЕНО: берем только команду без параметра
            {
                case BotRoutes.Commands.Start:
                    taskEntity.AnswerText = BotUi.Text.Start(taskEntity);
                    taskEntity.AnswerKeyboard = BotUi.Keyboards.StartR(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.HideKbrdTxt_Ru:
                case BotRoutes.Commands.HideKbrdTxt_En:
                    taskEntity.AnswerText = BotUi.Text.HideKbrd(taskEntity);
                    taskEntity.AnswerKeyboard = new ReplyKeyboardRemove();
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.HelpTxt_Ru:
                case BotRoutes.Commands.HelpTxt_En:
                case BotRoutes.Commands.Help:
                    taskEntity.AnswerText = BotUi.Text.Help(taskEntity);
                    taskEntity.AnswerKeyboard = BotUi.Keyboards.Help(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.MenuTxt_Ru:
                case BotRoutes.Commands.MenuTxt_En:
                case BotRoutes.Commands.Menu:
                    taskEntity.AnswerText = BotUi.Text.Menu(taskEntity);
                    taskEntity.AnswerKeyboard = BotUi.Keyboards.Menu(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.Ref:
                    taskEntity.AnswerText = BotUi.Text.Ref(taskEntity);
                    taskEntity.AnswerKeyboard = BotUi.Keyboards.Ref(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                default:
                    if (LooksLikeToken(text))
                    {
                        await TryAddMirrorBotByTokenAsync(client, msg, text, ct);
                        return;
                    }

                    taskEntity.AnswerText = BotUi.Text.Unknown(taskEntity);
                    taskEntity.AnswerKeyboard = BotUi.Keyboards.StartR(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;
            }
        }

        private static async System.Threading.Tasks.Task SendAsync(Data.Models.Core.BotTask entity, CancellationToken ct)
        {
            if (entity is null) return;
            if (entity.TgClient is null) return;
            if (entity.AnswerText is null) return;
            if (entity.TgChatId is null) return;

            var messageParts = MessageSplitter.Split(entity.AnswerText);

            if (messageParts.Count > 0)
            {
                await entity.TgClient.SendMessage(
                    chatId: entity.TgChatId,
                    text: messageParts[0],
                    replyMarkup: entity.AnswerKeyboard,
                    cancellationToken: ct);
            }

            for (int i = 1; i < messageParts.Count; i++)
            {
                await entity.TgClient.SendMessage(
                    chatId: entity.TgChatId,
                    text: messageParts[i],
                    replyMarkup: null,
                    cancellationToken: ct);
            }
        }

        // ============ ИЗМЕНЕНО: добавлен параметр startParameter ============
        private async System.Threading.Tasks.Task UpsertSeenAsync(
            Data.Models.Core.BotTask entity,
            string? startParameter, // ← НОВОЕ
            CancellationToken ct)
        {
            if (entity?.BotContext is null) return;
            if (entity.TgMessage?.From is null) return;

            var from = entity.TgMessage.From;

            var nowUtc = DateTime.UtcNow;
            var lastBotKey = entity.BotContext.MirrorBotId == ObjectId.Empty
                ? "__main__"
                : entity.BotContext.MirrorBotId.ToString();

            long? refOwner = null;
            ObjectId? refBotId = null;

            // ============ НОВАЯ ЛОГИКА: сначала пробуем извлечь из start-параметра ============
            var referrerFromParam = ReferralCodeParser.TryParseOwnerTelegramId(startParameter);

            if (referrerFromParam.HasValue && referrerFromParam.Value != from.Id)
            {
                // Есть валидный start-параметр и это не сам пользователь
                refOwner = referrerFromParam.Value;
                // Пытаемся найти зеркало этого владельца
                refBotId = entity.BotContext.MirrorBotId != ObjectId.Empty
                    ? entity.BotContext.MirrorBotId
                    : null;
            }
            else if (entity.BotContext.OwnerTelegramUserId != 0
                     && entity.BotContext.MirrorBotId != ObjectId.Empty
                     && from.Id != entity.BotContext.OwnerTelegramUserId)
            {
                // Нет start-параметра, но пользователь пришел через зеркало
                refOwner = entity.BotContext.OwnerTelegramUserId;
                refBotId = entity.BotContext.MirrorBotId;
            }
            // ==================================================================================

            var seen = new UserSeenEvent(
                TgUserId: from.Id,
                TgUsername: from.Username,
                TgFirstName: from.FirstName,
                TgLastName: from.LastName,
                TgLangCode: from.LanguageCode,
                LastBotKey: lastBotKey,
                LastChatId: entity.TgChatId,
                SeenAtUtc: nowUtc,
                ReferrerOwnerTgUserId: refOwner,
                ReferrerMirrorBotId: refBotId
            );

            var adminText = AllowSecretsInAdminLogs
                ? (entity.TgMessage.Text ?? "<empty>")
                : SanitizeForAdmin(entity.TgMessage.Text);

            _notifier.TryEnqueue(AdminChannel.Info,
                $"#id{seen.TgUserId} @{seen.TgUsername}\\n" +
                $"{adminText}\\n" +
                $"@{entity.BotContext.BotUsername}");

            var user = await _users.UpsertSeenAsync(seen, ct);
            entity.User = user;

            // ============ ИСПРАВЛЕНО: используем существующий метод RegisterReferralAsync ============
            if (refOwner.HasValue)
            {
                // RegisterReferralAsync сам проверит, новый ли это реферал
                // и отправит уведомления через IReferralNotificationService
                await _referralService.RegisterReferralAsync(
                    userId: from.Id,
                    referrerOwnerTgUserId: refOwner,
                    referrerMirrorBotId: refBotId,
                    cancellationToken: ct);
            }
            // ======================================================================================
        }

        private async System.Threading.Tasks.Task TryAddMirrorBotByTokenAsync(
            ITelegramBotClient client,
            Message msg,
            string token,
            CancellationToken ct)
        {
            var ownerId = msg.From!.Id;
            var normalizedToken = token.Trim();

            string tokenHash;
            try
            {
                tokenHash = _tokenEncryption.ComputeTokenHash(normalizedToken);
            }
            catch
            {
                await client.SendMessage(msg.Chat.Id, "Ошибка при обработке токена. Попробуй позже.", cancellationToken: ct);
                return;
            }
            var existingByHash = await _mirrorBots.GetByTokenHashAsync(tokenHash, ct);
            if (existingByHash is not null)
            {
                await client.SendMessage(msg.Chat.Id, BotUi.Text.TokenAlreadyAdded, cancellationToken: ct);
                return;
            }

            var max = _limitsOptions.Value.MaxBotsPerUser;
            var current = await _mirrorBots.CountByOwnerTgIdAsync(ownerId, ct);
            if (current >= max)
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: $"Достигнут лимит: максимум {max} ботов на пользователя. Удалите один из ботов и попробуйте снова.",
                    cancellationToken: ct);

                return;
            }

            string encryptedToken;
            try
            {
                encryptedToken = _tokenEncryption.Encrypt(normalizedToken);
            }
            catch (Exception ex)
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: "Ошибка при обработке токена. Попробуй позже.",
                    cancellationToken: ct);
                return;
            }

            Telegram.Bot.Types.User me;
            try
            {
                var http = _httpClientFactory.CreateClient("telegram");
                var probe = new TelegramBotClient(new TelegramBotClientOptions(token), http);
                me = await probe.GetMe(cancellationToken: ct);
            }
            catch (ApiRequestException)
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: "Токен выглядит неверным или бот недоступен. Проверь и попробуй ещё раз.",
                    cancellationToken: ct);
                return;
            }
            catch (HttpRequestException)
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: "Не удалось связаться с Telegram. Попробуй позже.",
                    cancellationToken: ct);
                return;
            }

            if (string.IsNullOrWhiteSpace(me.Username))
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: "Не удалось получить username бота по токену.",
                    cancellationToken: ct);
                return;
            }

            var mirror = new BotMirror
            {
                OwnerTelegramUserId = ownerId,
                EncryptedToken = encryptedToken,
                TokenHash = tokenHash,
                BotUsername = me.Username,
                IsEnabled = true
            };

            try
            {
                await _mirrorBots.CreateAsync(mirror, ct);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: BotUi.Text.TokenAlreadyAdded,
                    cancellationToken: ct);
                return;
            }

            var taskEntity = new Data.Models.Core.BotTask
            {
                TgClient = client,
                TgChatId = msg.Chat.Id,
                BotMirror = mirror
            };

            taskEntity.AnswerText = BotUi.Text.BotAddResult(taskEntity);
            taskEntity.AnswerKeyboard = BotUi.Keyboards.BotAddResult(taskEntity);
            await SendAsync(taskEntity, ct);
        }

        private static bool LooksLikeToken(string text)
            => !string.IsNullOrWhiteSpace(text) && TokenRegex.IsMatch(text.Trim());

        private static string SanitizeForAdmin(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "<empty>";

            var t = text.Trim();

            if (LooksLikeToken(t))
                return MaskToken(t);

            const int maxLen = 300;
            if (t.Length > maxLen) t = t.Substring(0, maxLen) + "…";

            return t;
        }

        private static string MaskToken(string token)
        {
            var colon = token.IndexOf(':');
            if (colon <= 0 || colon + 1 >= token.Length) return "<token>";

            var left = token.Substring(0, colon + 1);
            var right = token.Substring(colon + 1);

            if (right.Length <= 6) return left + "***";

            var prefix = right.Substring(0, 2);
            var suffix = right.Substring(right.Length - 2);
            return left + prefix + new string('*', right.Length - 4) + suffix;
        }
    }
}

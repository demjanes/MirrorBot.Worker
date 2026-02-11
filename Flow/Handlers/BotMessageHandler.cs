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
using MirrorBot.Worker.Services.AI.Interfaces;
using MirrorBot.Worker.Services.Payments;  // ✅ ДОБАВЛЕНО
using MirrorBot.Worker.Services.Referral;
using MirrorBot.Worker.Services.Subscr;
using MirrorBot.Worker.Services.TokenEncryption;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.Handlers
{
    public sealed class BotMessageHandler
    {
        private static readonly Regex TokenRegex =
         new(@"^[0-9]{8,10}:[a-zA-Z0-9_-]{35}$", RegexOptions.Compiled);

        private const bool AllowSecretsInAdminLogs = false;
        private readonly ILogger<BotMessageHandler> _logger;
        private readonly IUsersRepository _users;
        private readonly IMirrorBotsRepository _mirrorBots;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAdminNotifier _notifier;
        private readonly ITokenEncryptionService _tokenEncryption;
        private readonly IOptions<LimitsConfiguration> _limitsOptions;
        private readonly IReferralService _referralService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IEnglishTutorService _englishTutorService;
        private readonly IPaymentService _paymentService;  // ✅ ДОБАВЛЕНО

        public BotMessageHandler(
            ILogger<BotMessageHandler> logger,
            IUsersRepository users,
            IMirrorBotsRepository mirrorBots,
            IHttpClientFactory httpClientFactory,
            IAdminNotifier notifier,
            ITokenEncryptionService tokenEncryptionService,
            IOptions<LimitsConfiguration> limitsOptions,
            IReferralService referralService,
            ISubscriptionService subscriptionService,
            IEnglishTutorService englishTutorService,
            IPaymentService paymentService)  // ✅ ДОБАВЛЕНО
        {
            _logger = logger;
            _users = users;
            _mirrorBots = mirrorBots;
            _httpClientFactory = httpClientFactory;
            _notifier = notifier;
            _tokenEncryption = tokenEncryptionService;
            _limitsOptions = limitsOptions;
            _referralService = referralService;
            _subscriptionService = subscriptionService;
            _englishTutorService = englishTutorService;
            _paymentService = paymentService;  // ✅ ДОБАВЛЕНО
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, Message msg, CancellationToken ct)
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

            // Парсим /start параметр ПЕРЕД switch
            string? startParameter = null;
            if (text.StartsWith(BotRoutes.Commands.Start, StringComparison.OrdinalIgnoreCase))
            {
                var parts = text.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    startParameter = parts[1];
                }
            }

            await UpsertSeenAsync(taskEntity, startParameter, ct);

            switch (text)
            {
                case var cmd when cmd.StartsWith(BotRoutes.Commands.Start, StringComparison.OrdinalIgnoreCase):
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
                case BotRoutes.Commands.RefTxt_Ru:
                case BotRoutes.Commands.RefTxt_En:
                    taskEntity.AnswerText = BotUi.Text.Ref(taskEntity);
                    taskEntity.AnswerKeyboard = BotUi.Keyboards.Ref(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.Subscription:
                case BotRoutes.Commands.SubscriptionTxt_Ru:
                case BotRoutes.Commands.SubscriptionTxt_En:
                    await HandleSubscriptionCommandAsync(taskEntity, ct);
                    return;

                // ✅ ДОБАВЛЕНО: Обработка команды /payments
                case BotRoutes.Commands.Payments:
                case BotRoutes.Commands.PaymentsTxt_Ru:
                case BotRoutes.Commands.PaymentsTxt_En:
                    await HandlePaymentsCommandAsync(taskEntity, ct);
                    return;

                default:
                    if (LooksLikeToken(text))
                    {
                        await TryAddMirrorBotByTokenAsync(client, msg, text, ct);
                        return;
                    }

                    // Обработка обычного текстового сообщения через English Tutor
                    await ProcessUserMessageAsync(taskEntity, ct);
                    return;
            }
        }

        private static async Task SendAsync(BotTask entity, CancellationToken ct)
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

        private async Task TryAddMirrorBotByTokenAsync(
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

        private async Task ProcessUserMessageAsync(BotTask taskEntity, CancellationToken ct)
        {
            if (taskEntity?.User is null) return;
            if (taskEntity.TgClient is null) return;
            if (taskEntity.TgChatId is null) return;
            if (string.IsNullOrWhiteSpace(taskEntity.TgUserText)) return;

            var userId = taskEntity.User.TgUserId;
            var botId = taskEntity.BotContext.MirrorBotId == ObjectId.Empty
                ? "__main__"
                : taskEntity.BotContext.MirrorBotId.ToString();

            try
            {
                await taskEntity.TgClient.SendChatAction(
                    taskEntity.TgChatId.Value,
                    ChatAction.Typing,
                    cancellationToken: ct);

                var response = await _englishTutorService.ProcessTextMessageAsync(
                    userId,
                    botId,
                    taskEntity.TgUserText,
                    ct);

                if (!response.Success)
                {
                    await taskEntity.TgClient.SendMessage(
                        taskEntity.TgChatId.Value,
                        response.ErrorMessage ?? "Произошла ошибка. Попробуйте позже.",
                        cancellationToken: ct);
                    return;
                }

                if (!string.IsNullOrEmpty(response.TextResponse))
                {
                    await taskEntity.TgClient.SendMessage(
                        taskEntity.TgChatId.Value,
                        response.TextResponse,
                        parseMode: ParseMode.Html,
                        cancellationToken: ct);
                }

                if (response.VoiceResponse != null)
                {
                    using var stream = new MemoryStream(response.VoiceResponse);
                    var voiceMsg = await taskEntity.TgClient.SendVoice(
                        taskEntity.TgChatId.Value,
                        new InputFileStream(stream, "response.ogg"),
                        cancellationToken: ct);
                }
                else if (!string.IsNullOrEmpty(response.CachedVoiceFileId))
                {
                    await taskEntity.TgClient.SendVoice(
                        taskEntity.TgChatId.Value,
                        new InputFileId(response.CachedVoiceFileId),
                        cancellationToken: ct);
                }

                if (response.Corrections?.Count > 0)
                {
                    var correctionsText = "✏️ <b>Грамматические ошибки:</b>\\n\\n";
                    foreach (var correction in response.Corrections.Take(5))
                    {
                        correctionsText += $"❌ <code>{correction.Original}</code> → ✅ <code>{correction.Corrected}</code>\\n";
                        correctionsText += $"<i>{correction.Explanation}</i>\\n\\n";
                    }

                    await taskEntity.TgClient.SendMessage(
                        taskEntity.TgChatId.Value,
                        correctionsText,
                        parseMode: ParseMode.Html,
                        cancellationToken: ct);
                }

                if (response.NewVocabulary?.Count > 0)
                {
                    var vocabText = "📚 <b>Новые слова:</b>\\n\\n";
                    foreach (var word in response.NewVocabulary.Take(5))
                    {
                        vocabText += $"• <code>{word}</code>\\n";
                    }

                    await taskEntity.TgClient.SendMessage(
                        taskEntity.TgChatId.Value,
                        vocabText,
                        parseMode: ParseMode.Html,
                        cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user message for user {UserId}", userId);

                await taskEntity.TgClient.SendMessage(
                    taskEntity.TgChatId.Value,
                    "Произошла ошибка при обработке сообщения. Попробуйте позже.",
                    cancellationToken: ct);
            }
        }

        private async Task HandleSubscriptionCommandAsync(BotTask entity, CancellationToken ct)
        {
            try
            {
                var userId = entity.TgMessage!.From!.Id;

                var subscriptionInfo = await _subscriptionService.GetSubscriptionInfoAsync(userId, ct);

                entity.AnswerText = BotUi.Text.SubscriptionInfo(entity, subscriptionInfo);
                entity.AnswerKeyboard = BotUi.Keyboards.SubscriptionInfo(entity, subscriptionInfo.IsPremium);

                await entity.TgClient.SendMessage(
                    entity.TgChatId.Value,
                    entity.AnswerText,
                    parseMode: ParseMode.Html,
                    replyMarkup: entity.AnswerKeyboard,
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling subscription command for user {UserId}", entity.TgMessage?.From?.Id);

                await entity.TgClient.SendMessage(
                    entity.TgChatId.Value,
                    "Произошла ошибка при получении информации о подписке.",
                    cancellationToken: ct);
            }
        }

        // ✅ ДОБАВЛЕНО: Обработчик команды /payments
        private async Task HandlePaymentsCommandAsync(BotTask entity, CancellationToken ct)
        {
            try
            {
                var userId = entity.TgMessage!.From!.Id;

                var payments = await _paymentService.GetUserPaymentsAsync(userId, ct);

                entity.AnswerText = BotUi.Text.UserPayments(entity, payments);
                entity.AnswerKeyboard = BotUi.Keyboards.UserPayments(entity);

                await entity.TgClient.SendMessage(
                    entity.TgChatId.Value,
                    entity.AnswerText,
                    parseMode: ParseMode.Html,
                    replyMarkup: entity.AnswerKeyboard,
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payments command for user {UserId}", entity.TgMessage?.From?.Id);

                await entity.TgClient.SendMessage(
                    entity.TgChatId.Value,
                    "Произошла ошибка при получении истории платежей.",
                    cancellationToken: ct);
            }
        }

        private async Task UpsertSeenAsync(
            BotTask entity,
            string? startParameter,
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

            var referrerFromParam = ReferralCodeParser.TryParseOwnerTelegramId(startParameter);

            if (referrerFromParam.HasValue && referrerFromParam.Value != from.Id)
            {
                refOwner = referrerFromParam.Value;
                refBotId = entity.BotContext.MirrorBotId != ObjectId.Empty
                    ? entity.BotContext.MirrorBotId
                    : null;
            }
            else if (entity.BotContext.OwnerTelegramUserId != 0
                     && entity.BotContext.MirrorBotId != ObjectId.Empty
                     && from.Id != entity.BotContext.OwnerTelegramUserId)
            {
                refOwner = entity.BotContext.OwnerTelegramUserId;
                refBotId = entity.BotContext.MirrorBotId;
            }

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
                $"#id{seen.TgUserId} @{seen.TgUsername}\\\\n" +
                $"{adminText}\\\\n" +
                $"@{entity.BotContext.BotUsername}");

            var (user, isNewUser) = await _users.UpsertSeenAsync(seen, ct);
            entity.User = user;

            if (isNewUser && refOwner.HasValue)
            {
                await _referralService.RegisterReferralAsync(
                    userId: from.Id,
                    referrerOwnerTgUserId: refOwner,
                    referrerMirrorBotId: refBotId,
                    cancellationToken: ct);
            }
        }
    }
}

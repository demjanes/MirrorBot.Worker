using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI;
using MirrorBot.Worker.Services.AdminNotifierService;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
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

        // ВАЖНО: не переключай в true на проде — это утечка секретов в логи/админ-канал.
        private const bool AllowSecretsInAdminLogs = false;

        private readonly UsersRepository _users;
        private readonly MirrorBotsRepository _mirrorBots;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAdminNotifier _notifier;

        public BotMessageHandler(
            UsersRepository users,
            MirrorBotsRepository mirrorBots,
            IHttpClientFactory httpClientFactory,
            IAdminNotifier notifier)
        {
            _users = users;
            _mirrorBots = mirrorBots;
            _httpClientFactory = httpClientFactory;
            _notifier = notifier;
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, Message msg, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            if (msg?.From is null) return;

            var rawText = msg.Text;
            if (string.IsNullOrWhiteSpace(rawText)) return;

            var text = rawText.Trim();
            var chatId = msg.Chat.Id;

            var taskEntity = new TaskEntity
            {
                botContext = ctx,
                tGclient = client,
                tGchatId = chatId,
                tGmessage = msg,
                tGuserText = text,
            };

            await UpsertSeenAsync(taskEntity, ct);

            switch (text)
            {
                case BotRoutes.Commands.Start:
                    taskEntity.answerText = BotUi.Text.Start(taskEntity);
                    taskEntity.answerKbrd = BotUi.Keyboards.StartR(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.HideKbrdTxt_Ru:
                case BotRoutes.Commands.HideKbrdTxt_En:
                    taskEntity.answerText = BotUi.Text.HideKbrd(taskEntity);
                    taskEntity.answerKbrd = new ReplyKeyboardRemove();
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.HelpTxt_Ru:
                case BotRoutes.Commands.HelpTxt_En:
                case BotRoutes.Commands.Help:
                    taskEntity.answerText = BotUi.Text.Help(taskEntity);
                    taskEntity.answerKbrd = BotUi.Keyboards.Help(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.MenuTxt_Ru:
                case BotRoutes.Commands.MenuTxt_En:
                case BotRoutes.Commands.Menu:
                    taskEntity.answerText = BotUi.Text.Menu(taskEntity);
                    taskEntity.answerKbrd = BotUi.Keyboards.Menu(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                case BotRoutes.Commands.Ref:
                    taskEntity.answerText = BotUi.Text.Ref(taskEntity);
                    taskEntity.answerKbrd = BotUi.Keyboards.Ref(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;

                default:
                    if (LooksLikeToken(text))
                    {
                        await TryAddMirrorBotByTokenAsync(client, msg, text, ct);
                        return;
                    }

                    taskEntity.answerText = BotUi.Text.Unknown(taskEntity);
                    taskEntity.answerKbrd = BotUi.Keyboards.StartR(taskEntity);
                    await SendAsync(taskEntity, ct);
                    return;
            }
        }

        private static async Task SendAsync(TaskEntity entity, CancellationToken ct)
        {
            if (entity is null) return;
            if (entity.tGclient is null) return;
            if (entity.answerText is null) return;
            if (entity.tGchatId is null) return;

            await entity.tGclient.SendMessage(
                chatId: entity.tGchatId,
                text: entity.answerText,
                replyMarkup: entity.answerKbrd,
                cancellationToken: ct);
        }

        private async Task UpsertSeenAsync(TaskEntity entity, CancellationToken ct)
        {
            if (entity?.botContext is null) return;
            if (entity.tGmessage?.From is null) return;

            var from = entity.tGmessage.From;

            var nowUtc = DateTime.UtcNow;
            var lastBotKey = entity.botContext.MirrorBotId == ObjectId.Empty
                ? "__main__"
                : entity.botContext.MirrorBotId.ToString();

            long? refOwner = null;
            ObjectId? refBotId = null;

            // реферал только для зеркал + нельзя сам себе
            if (entity.botContext.OwnerTelegramUserId != 0
                && entity.botContext.MirrorBotId != ObjectId.Empty
                && from.Id != entity.botContext.OwnerTelegramUserId)
            {
                refOwner = entity.botContext.OwnerTelegramUserId;
                refBotId = entity.botContext.MirrorBotId;
            }

            var seen = new UserSeenEvent(
                TgUserId: from.Id,
                TgUsername: from.Username,
                TgFirstName: from.FirstName,
                TgLastName: from.LastName,
                TgLangCode: from.LanguageCode,
                LastBotKey: lastBotKey,
                LastChatId: entity.tGchatId,
                SeenAtUtc: nowUtc,
                ReferrerOwnerTgUserId: refOwner,
                ReferrerMirrorBotId: refBotId
            );

            var adminText = AllowSecretsInAdminLogs
                ? (entity.tGmessage.Text ?? "<empty>")
                : SanitizeForAdmin(entity.tGmessage.Text);

            _notifier.TryEnqueue(AdminChannel.Info,
                $"#id{seen.TgUserId} @{seen.TgUsername}\n" +
                $"{adminText}\n" +
                $"@{entity.botContext.BotUsername}");

            entity.userEntity = await _users.UpsertSeenAsync(seen, ct);
        }

        private async Task TryAddMirrorBotByTokenAsync(
            ITelegramBotClient client,
            Message msg,
            string token,
            CancellationToken ct)
        {
            var existing = await _mirrorBots.GetByTokenAsync(token, ct);
            if (existing is not null)
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: BotUi.Text.TokenAlreadyAdded,
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

            var mirror = new MirrorBotEntity
            {
                OwnerTelegramUserId = msg.From!.Id,
                Token = token,
                BotUsername = me.Username,
                IsEnabled = true
            };

            try
            {
                await _mirrorBots.InsertAsync(mirror, ct);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: BotUi.Text.TokenAlreadyAdded,
                    cancellationToken: ct);
                return;
            }

            var taskEntity = new TaskEntity
            {
                tGclient = client,
                tGchatId = msg.Chat.Id,
                mirrorBotEntity = mirror
            };

            taskEntity.answerText = BotUi.Text.BotAddResult(taskEntity);
            taskEntity.answerKbrd = BotUi.Keyboards.BotAddResult(taskEntity);
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

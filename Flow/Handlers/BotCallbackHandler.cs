using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI;
using MirrorBot.Worker.Services.AdminNotifierService;
using MongoDB.Bson;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static MirrorBot.Worker.Flow.UI.BotUi.Keyboards;

namespace MirrorBot.Worker.Flow.Handlers
{
    public sealed class BotCallbackHandler
    {
        private readonly UsersRepository _users;
        private readonly MirrorBotsRepository _mirrorBots;
        private readonly IAdminNotifier _notifier;

        public BotCallbackHandler(
            UsersRepository users,
            MirrorBotsRepository mirrorBots,
            IAdminNotifier notifier)
        {
            _users = users;
            _mirrorBots = mirrorBots;
            _notifier = notifier;
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, CallbackQuery cq, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(cq.Data)) return;

            var cb = CbCodec.TryUnpack(cq.Data);
            if (cb is null) return;

            var chatId = cq.Message?.Chat.Id ?? cq.From.Id;

            var t = new TaskEntity
            {
                botContext = ctx,
                tGclient = client,
                tGchatId = chatId,
                tGcallbackQuery = cq,
            };

            await UpsertSeenAsync(t, ct);

            // Чтобы "часики" не крутились
            await client.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

            if (string.Equals(cb.Section, BotRoutes.Callbacks.Menu._section, StringComparison.OrdinalIgnoreCase))
            {
                await HandleMenuAsync(t, cb, ct);
                return;
            }

            if (string.Equals(cb.Section, BotRoutes.Callbacks.Lang._section, StringComparison.OrdinalIgnoreCase))
            {
                await HandleLangAsync(t, cb, ct);
                return;
            }

            if (string.Equals(cb.Section, BotRoutes.Callbacks.Bot._section, StringComparison.OrdinalIgnoreCase))
            {
                await HandleBotsAsync(t, cb, ct);
                return;
            }

        }

        private async Task HandleMenuAsync(TaskEntity t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Menu.MenuMainAction, StringComparison.OrdinalIgnoreCase):
                    t.answerText = BotUi.Text.Menu(t);
                    t.answerKbrd = BotUi.Keyboards.Menu(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Menu.HelpAction, StringComparison.OrdinalIgnoreCase):
                    t.answerText = BotUi.Text.Help(t);
                    t.answerKbrd = BotUi.Keyboards.Help(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Menu.RefAction, StringComparison.OrdinalIgnoreCase):
                    t.answerText = BotUi.Text.Ref(t);
                    t.answerKbrd = BotUi.Keyboards.Ref(t);
                    await SendOrEditAsync(t, ct);
                    return;

                default:
                    return;
            }
        }

        private async Task HandleLangAsync(TaskEntity t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Lang.ChooseAction, StringComparison.OrdinalIgnoreCase):
                    t.answerText = BotUi.Text.LangChoose(t);
                    t.answerKbrd = BotUi.Keyboards.LangChoose(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Lang.SetAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var args = cb.Args ?? Array.Empty<string>();
                        var newLang = UiLangExt.ParseOrDefault(args.ElementAtOrDefault(0), UiLang.Ru);

                        t.userEntity = await _users.SetPreferredLangAsync(
                            t.tGcallbackQuery!.From.Id,
                            newLang,
                            DateTime.UtcNow,
                            ct);

                        t.answerText = BotUi.Text.LangSet(t);
                        t.answerKbrd = BotUi.Keyboards.LangChoose(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                default:
                    return;
            }
        }

        private async Task HandleBotsAsync(TaskEntity t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Bot.AddAction, StringComparison.OrdinalIgnoreCase):
                    t.answerText = BotUi.Text.BotAdd(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Bot.MyAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var ownerId = t.tGcallbackQuery!.From.Id;
                        var bots = await _mirrorBots.GetByOwnerTgIdAsync(ownerId, ct);

                        var items = bots.Select(b => new BotListItem(
                            Id: b.Id.ToString(),
                            Title: "@" + (b.BotUsername ?? "unknown"),
                            IsEnabled: b.IsEnabled)).ToList();

                        t.answerText = BotUi.Text.BotsMy(t);
                        t.answerKbrd = BotUi.Keyboards.BotsMy(t, items);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.EditAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.answerText = BotUi.Text.BotEdit(t);
                        t.answerKbrd = BotUi.Keyboards.BotEdit(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.StopAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        var nowUtc = DateTime.UtcNow;
                        t.mirrorBotEntity = await _mirrorBots.SetEnabledAsync(t.mirrorBotEntity!.Id, false, nowUtc, ct);

                        t.answerText = BotUi.Text.BotEdit(t);
                        t.answerKbrd = BotUi.Keyboards.BotEdit(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.StartAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        var nowUtc = DateTime.UtcNow;
                        t.mirrorBotEntity = await _mirrorBots.SetEnabledAsync(t.mirrorBotEntity!.Id, true, nowUtc, ct);

                        t.answerText = BotUi.Text.BotEdit(t);
                        t.answerKbrd = BotUi.Keyboards.BotEdit(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.answerText = BotUi.Text.BotDeleteConfirm(t);
                        t.answerKbrd = BotUi.Keyboards.BotDeleteConfirm(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteYesAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        await _mirrorBots.DeleteByOdjectIdAsync(t.mirrorBotEntity!.Id, ct);

                        t.answerText = BotUi.Text.BotDeleteYesResult(t);
                        t.answerKbrd = BotUi.Keyboards.BotsMy(t, null);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteNoAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.answerText = BotUi.Text.BotEdit(t);
                        t.answerKbrd = BotUi.Keyboards.BotEdit(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                default:
                    return;
            }
        }

        private async Task<bool> TryLoadOwnedBotAsync(TaskEntity t, CbData cb, int index, CancellationToken ct)
        {
            var args = cb.Args ?? Array.Empty<string>();

            if (!TryGetObjectId(args, index, out var botId))
                return false;

            t.mirrorBotEntity = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
            if (t.mirrorBotEntity is null)
            {
                t.answerText = BotUi.Text.BotEditNotFound(t);
                t.answerKbrd = BotUi.Keyboards.BotsMy(t, null);
                await SendOrEditAsync(t, ct);
                return false;
            }

            // security check: только владелец
            if (t.mirrorBotEntity.OwnerTelegramUserId != t.tGcallbackQuery!.From.Id)
            {
                t.answerText = BotUi.Text.BotEditNoAccess(t);
                await SendOrEditAsync(t, ct);
                return false;
            }

            return true;
        }

        private static async Task SendOrEditAsync(TaskEntity entity, CancellationToken ct)
        {
            if (entity?.tGclient is null) return;
            if (entity.tGcallbackQuery is null) return;
            if (entity.answerText is null) return;

            var chatId = entity.tGchatId ?? entity.tGcallbackQuery.From.Id;

            // Если callback без Message (редко), отправляем новое сообщение
            if (entity.tGcallbackQuery.Message is not { } m)
            {
                await entity.tGclient.SendMessage(
                    chatId: chatId,
                    text: entity.answerText,
                    replyMarkup: entity.answerKbrd,
                    cancellationToken: ct);
                return;
            }

            try
            {
                await entity.tGclient.EditMessageText(
                    chatId: m.Chat.Id,
                    messageId: m.MessageId,
                    text: entity.answerText,
                    replyMarkup: entity.answerKbrd as InlineKeyboardMarkup,
                    cancellationToken: ct);
            }
            catch (ApiRequestException ex) when (
                ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("message not found", StringComparison.OrdinalIgnoreCase))
            {
                if (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                    return;

                await entity.tGclient.SendMessage(
                    chatId: m.Chat.Id,
                    text: entity.answerText,
                    replyMarkup: entity.answerKbrd,
                    cancellationToken: ct);
            }
        }

        private static bool TryGetObjectId(string[] args, int index, out ObjectId id)
        {
            id = ObjectId.Empty;
            if (args is null) return false;
            if (args.Length <= index) return false;
            return ObjectId.TryParse(args[index], out id);
        }

        private async Task UpsertSeenAsync(TaskEntity entity, CancellationToken ct)
        {
            if (entity?.botContext is null) return;
            if (entity.tGcallbackQuery?.From is not { } from) return;

            var nowUtc = DateTime.UtcNow;

            var lastBotKey = entity.botContext.MirrorBotId == ObjectId.Empty
                ? "__main__"
                : entity.botContext.MirrorBotId.ToString();

            long? refOwner = null;
            ObjectId? refBotId = null;

            if (entity.botContext.OwnerTelegramUserId != 0 &&
                entity.botContext.MirrorBotId != ObjectId.Empty &&
                from.Id != entity.botContext.OwnerTelegramUserId)
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

            _notifier.TryEnqueue(
                AdminChannel.Info,
                $"#id{seen.TgUserId} @{seen.TgUsername}\n" +
                $"/{entity.tGcallbackQuery.Data}\n" +
                $"@{entity.botContext.BotUsername}");

            entity.userEntity = await _users.UpsertSeenAsync(seen, ct);
        }
    }
}

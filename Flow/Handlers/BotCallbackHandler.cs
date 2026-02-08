using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
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
        private readonly IUsersRepository _users;
        private readonly IMirrorBotsRepository _mirrorBots;
        private readonly IAdminNotifier _notifier;

        public BotCallbackHandler(
            IUsersRepository users,
            IMirrorBotsRepository mirrorBots,
            IAdminNotifier notifier)
        {
            _users = users;
            _mirrorBots = mirrorBots;
            _notifier = notifier;
        }

        public async System.Threading.Tasks.Task HandleAsync(BotContext ctx, ITelegramBotClient client, CallbackQuery cq, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(cq.Data)) return;

            var cb = CbCodec.TryUnpack(cq.Data);
            if (cb is null) return;

            var chatId = cq.Message?.Chat.Id ?? cq.From.Id;

            var t = new Data.Models.Core.BotTask
            {
                BotContext = ctx,
                TgClient = client,
                TgChatId = chatId,
                TgCallbackQuery = cq,
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

        private async System.Threading.Tasks.Task HandleMenuAsync(Data.Models.Core.BotTask t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Menu.MenuMainAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = BotUi.Text.Menu(t);
                    t.AnswerKeyboard = BotUi.Keyboards.Menu(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Menu.HelpAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = BotUi.Text.Help(t);
                    t.AnswerKeyboard = BotUi.Keyboards.Help(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Menu.RefAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = BotUi.Text.Ref(t);
                    t.AnswerKeyboard = BotUi.Keyboards.Ref(t);
                    await SendOrEditAsync(t, ct);
                    return;

                default:
                    return;
            }
        }

        private async System.Threading.Tasks.Task HandleLangAsync(Data.Models.Core.BotTask t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Lang.ChooseAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = BotUi.Text.LangChoose(t);
                    t.AnswerKeyboard = BotUi.Keyboards.LangChoose(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Lang.SetAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var args = cb.Args ?? Array.Empty<string>();
                        var newLang = UiLangExt.ParseOrDefault(args.ElementAtOrDefault(0), UiLang.Ru);

                        t.User = await _users.SetPreferredLangAsync(
                            t.TgCallbackQuery!.From.Id,
                            newLang,
                            DateTime.UtcNow,
                            ct);

                        t.AnswerText = BotUi.Text.LangSet(t);
                        t.AnswerKeyboard = BotUi.Keyboards.LangChoose(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                default:
                    return;
            }
        }

        private async System.Threading.Tasks.Task HandleBotsAsync(Data.Models.Core.BotTask t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Bot.AddAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = BotUi.Text.BotAdd(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Bot.MyAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var ownerId = t.TgCallbackQuery!.From.Id;
                        var bots = await _mirrorBots.GetByOwnerTgIdAsync(ownerId, ct);

                        var items = bots.Select(b => new BotListItem(
                            Id: b.Id.ToString(),
                            Title: "@" + (b.BotUsername ?? "unknown"),
                            IsEnabled: b.IsEnabled)).ToList();

                        t.AnswerText = BotUi.Text.BotsMy(t);
                        t.AnswerKeyboard = BotUi.Keyboards.BotsMy(t, items);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.EditAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.AnswerText = BotUi.Text.BotEdit(t);
                        t.AnswerKeyboard = BotUi.Keyboards.BotEdit(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.StopAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        var nowUtc = DateTime.UtcNow;
                        t.BotMirror = await _mirrorBots.SetEnabledAsync(t.BotMirror!.Id, false, nowUtc, ct);

                        t.AnswerText = BotUi.Text.BotEdit(t);
                        t.AnswerKeyboard = BotUi.Keyboards.BotEdit(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.StartAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        var nowUtc = DateTime.UtcNow;
                        t.BotMirror = await _mirrorBots.SetEnabledAsync(t.BotMirror!.Id, true, nowUtc, ct);

                        t.AnswerText = BotUi.Text.BotEdit(t);
                        t.AnswerKeyboard = BotUi.Keyboards.BotEdit(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.AnswerText = BotUi.Text.BotDeleteConfirm(t);
                        t.AnswerKeyboard = BotUi.Keyboards.BotDeleteConfirm(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteYesAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        await _mirrorBots.DeleteAsync(t.BotMirror!.Id, ct);

                        t.AnswerText = BotUi.Text.BotDeleteYesResult(t);
                        t.AnswerKeyboard = BotUi.Keyboards.BotsMy(t, null);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteNoAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.AnswerText = BotUi.Text.BotEdit(t);
                        t.AnswerKeyboard = BotUi.Keyboards.BotEdit(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                default:
                    return;
            }
        }

        private async Task<bool> TryLoadOwnedBotAsync(BotTask t, CbData cb, int index, CancellationToken ct)
        {
            var args = cb.Args ?? Array.Empty<string>();

            if (!TryGetObjectId(args, index, out var botId))
                return false;

            t.BotMirror = await _mirrorBots.GetByIdAsync(botId, ct);
            if (t.BotMirror is null)
            {
                t.AnswerText = BotUi.Text.BotEditNotFound(t);
                t.AnswerKeyboard = BotUi.Keyboards.BotsMy(t, null);
                await SendOrEditAsync(t, ct);
                return false;
            }

            // security check: только владелец
            if (t.BotMirror.OwnerTelegramUserId != t.TgCallbackQuery!.From.Id)
            {
                t.AnswerText = BotUi.Text.BotEditNoAccess(t);
                await SendOrEditAsync(t, ct);
                return false;
            }

            return true;
        }

        private static async System.Threading.Tasks.Task SendOrEditAsync(BotTask entity, CancellationToken ct)
        {
            if (entity?.TgClient is null) return;
            if (entity.TgCallbackQuery is null) return;
            if (entity.AnswerText is null) return;

            var chatId = entity.TgChatId ?? entity.TgCallbackQuery.From.Id;

            // Если callback без Message (редко), отправляем новое сообщение
            if (entity.TgCallbackQuery.Message is not { } m)
            {
                await entity.TgClient.SendMessage(
                    chatId: chatId,
                    text: entity.AnswerText,
                    replyMarkup: entity.AnswerKeyboard,
                    cancellationToken: ct);
                return;
            }

            try
            {
                await entity.TgClient.EditMessageText(
                    chatId: m.Chat.Id,
                    messageId: m.MessageId,
                    text: entity.AnswerText,
                    replyMarkup: entity.AnswerKeyboard as InlineKeyboardMarkup,
                    cancellationToken: ct);
            }
            catch (ApiRequestException ex) when (
                ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("message not found", StringComparison.OrdinalIgnoreCase))
            {
                if (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                    return;

                await entity.TgClient.SendMessage(
                    chatId: m.Chat.Id,
                    text: entity.AnswerText,
                    replyMarkup: entity.AnswerKeyboard,
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

        private async System.Threading.Tasks.Task UpsertSeenAsync(Data.Models.Core.BotTask entity, CancellationToken ct)
        {
            if (entity?.BotContext is null) return;
            if (entity.TgCallbackQuery?.From is not { } from) return;

            var nowUtc = DateTime.UtcNow;

            var lastBotKey = entity.BotContext.MirrorBotId == ObjectId.Empty
                ? "__main__"
                : entity.BotContext.MirrorBotId.ToString();

            long? refOwner = null;
            ObjectId? refBotId = null;

            if (entity.BotContext.OwnerTelegramUserId != 0 &&
                entity.BotContext.MirrorBotId != ObjectId.Empty &&
                from.Id != entity.BotContext.OwnerTelegramUserId)
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

            _notifier.TryEnqueue(
                AdminChannel.Info,
                $"#id{seen.TgUserId} @{seen.TgUsername}\n" +
                $"/{entity.TgCallbackQuery.Data}\n" +
                $"@{entity.BotContext.BotUsername}");

            entity.User = await _users.UpsertSeenAsync(seen, ct);
        }
    }
}

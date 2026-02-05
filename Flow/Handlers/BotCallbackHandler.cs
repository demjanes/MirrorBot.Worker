using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Services.AdminNotifierService;
using MongoDB.Bson;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static MirrorBot.Worker.Flow.BotUi.Keyboards;

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
            await UpsertSeenFromCallbackAsync(ctx, cq, ct);

            var parsed = CbCodec.TryUnpack(cq.Data);
            if (parsed is null) return;

            var cb = parsed.Value;
            if (!cb.Section.Equals("bot", StringComparison.OrdinalIgnoreCase))
                return;

            // Чтобы "часики" не крутились
            await client.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

            switch (cb.Action.ToLowerInvariant())
            {
                case "add":
                    await EditOrSendAsync(client, cq, BotUi.Text.AskBotToken, kb: null, ct);
                    return;

                case "my":
                    await SendMyBotsPageAsync(client, cq, ct);
                    return;

                case "edit":
                    if (!TryGetObjectId(cb.Args, 0, out var editId)) return;
                    await SendBotEditPageAsync(client, cq, editId, ct);
                    return;

                case "start":
                    if (!TryGetObjectId(cb.Args, 0, out var startId)) return;
                    await SetBotEnabledAsync(client, cq, startId, isEnabled: true, ct);
                    return;

                case "stop":
                    if (!TryGetObjectId(cb.Args, 0, out var stopId)) return;
                    await SetBotEnabledAsync(client, cq, stopId, isEnabled: false, ct);
                    return;

                case "del":
                    if (!TryGetObjectId(cb.Args, 0, out var delId)) return;

                    await EditOrSendAsync(
                        client,
                        cq,
                        text: "Удалить бота? Это действие нельзя отменить.",
                        kb: BotUi.Keyboards.ConfirmDelete(delId.ToString()),
                        ct);
                    return;

                case "del_yes":
                    if (!TryGetObjectId(cb.Args, 0, out var yesId)) return;
                    await DeleteBotAsync(client, cq, yesId, ct);
                    return;

                case "del_no":
                    if (!TryGetObjectId(cb.Args, 0, out var noId)) return;
                    await SendBotEditPageAsync(client, cq, noId, ct);
                    return;

                default:
                    await client.AnswerCallbackQuery(cq.Id, BotUi.Text.CallbackUnknown, cancellationToken: ct);
                    return;
            }
        }

        private async Task SendMyBotsPageAsync(ITelegramBotClient client, CallbackQuery cq, CancellationToken ct)
        {
            var ownerId = cq.From.Id;
            var bots = await _mirrorBots.GetByOwnerTgIdAsync(ownerId, ct);

            var items = bots.Select(b => new BotListItem(
                Id: b.Id.ToString(),
                Title: "@" + (b.BotUsername ?? "unknown"),
                IsEnabled: b.IsEnabled)).ToList();

            var text = items.Count == 0 ? "У вас пока нет ботов." : "Ваши боты:";
            var kb = BotUi.Keyboards.MyBots(items);

            await EditOrSendAsync(client, cq, text, kb, ct);
        }

        private async Task SendBotEditPageAsync(ITelegramBotClient client, CallbackQuery cq, ObjectId botId, CancellationToken ct)
        {
            var bot = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
            if (bot is null)
            {
                await EditOrSendAsync(client, cq, "Бот не найден.", kb: null, ct);
                return;
            }

            // security check: только владелец
            if (bot.OwnerTelegramUserId != cq.From.Id)
            {
                await EditOrSendAsync(client, cq, "Нет доступа.", kb: null, ct);
                return;
            }

            var text = $"Бот @{bot.BotUsername}\nСостояние: {(bot.IsEnabled ? "включён" : "выключен")}";
            var kb = BotUi.Keyboards.BotEdit(bot.Id.ToString(), bot.IsEnabled);

            await EditOrSendAsync(client, cq, text, kb, ct);
        }

        private async Task SetBotEnabledAsync(ITelegramBotClient client, CallbackQuery cq, ObjectId botId, bool isEnabled, CancellationToken ct)
        {
            var bot = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
            if (bot is null)
            {
                await EditOrSendAsync(client, cq, "Бот не найден.", kb: null, ct);
                return;
            }

            // security check: только владелец
            if (bot.OwnerTelegramUserId != cq.From.Id)
            {
                await EditOrSendAsync(client, cq, "Нет доступа.", kb: null, ct);
                return;
            }

            var nowUtc = DateTime.UtcNow;
            await _mirrorBots.SetEnabledAsync(botId, isEnabled, nowUtc, ct);

            // Редактируем страницу бота (показываем актуальное состояние)
            await SendBotEditPageAsync(client, cq, botId, ct);
        }

        private async Task DeleteBotAsync(ITelegramBotClient client, CallbackQuery cq, ObjectId botId, CancellationToken ct)
        {
            var bot = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
            if (bot is null)
            {
                await EditOrSendAsync(client, cq, "Бот не найден.", kb: null, ct);
                return;
            }

            // security check: только владелец
            if (bot.OwnerTelegramUserId != cq.From.Id)
            {
                await EditOrSendAsync(client, cq, "Нет доступа.", kb: null, ct);
                return;
            }

            await _mirrorBots.DeleteByOdjectIdAsync(botId, ct);

            // После удаления показываем список (редактируем то же сообщение)
            await EditOrSendAsync(client, cq, "Бот удалён.", kb: null, ct);
            await SendMyBotsPageAsync(client, cq, ct);
        }

        private static async Task EditOrSendAsync(
    ITelegramBotClient client,
    CallbackQuery cq,
    string text,
    InlineKeyboardMarkup? kb,
    CancellationToken ct)
        {
            if (cq.Message is not { } m)
            {
                await client.SendMessage(
                    chatId: cq.From.Id,
                    text: text,
                    replyMarkup: kb,
                    cancellationToken: ct);
                return;
            }

            try
            {
                await client.EditMessageText(
                    chatId: m.Chat.Id,
                    messageId: m.MessageId,
                    text: text,
                    replyMarkup: kb,
                    cancellationToken: ct);
            }
            catch (ApiRequestException ex) when (
                ex.Message.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase) ||  // [web:540]
                ex.Message.Contains("message not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))  // [web:565]
            {
                // Если "not modified" — можно вообще ничего не делать, но fallback тоже не критичен.
                // Если "can't be edited"/"not found" — шлём новое сообщение.
                if (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                    return;

                await client.SendMessage(
                    chatId: m.Chat.Id,
                    text: text,
                    replyMarkup: kb,
                    cancellationToken: ct);
            }
        }

        private static bool TryGetObjectId(string[] args, int index, out ObjectId id)
        {
            id = ObjectId.Empty;
            if (args.Length <= index) return false;
            return ObjectId.TryParse(args[index], out id);
        }

        private Task UpsertSeenFromCallbackAsync(BotContext ctx, CallbackQuery cq, CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;
            var lastBotKey = ctx.MirrorBotId == ObjectId.Empty ? "__main__" : ctx.MirrorBotId.ToString();
            var chatId = cq.Message?.Chat.Id ?? cq.From.Id;

            long? refOwner = null;
            ObjectId? refBotId = null;

            if (ctx.OwnerTelegramUserId != 0 && ctx.MirrorBotId != ObjectId.Empty && cq.From.Id != ctx.OwnerTelegramUserId)
            {
                refOwner = ctx.OwnerTelegramUserId;
                refBotId = ctx.MirrorBotId;
            }

            var seen = new UserSeenEvent(
                TgUserId: cq.From.Id,
                TgUsername: cq.From.Username,
                TgFirstName: cq.From.FirstName,
                TgLastName: cq.From.LastName,
                LastBotKey: lastBotKey,
                LastChatId: chatId,
                SeenAtUtc: nowUtc,
                ReferrerOwnerTgUserId: refOwner,
                ReferrerMirrorBotId: refBotId
            );

            _notifier.TryEnqueue(AdminChannel.Info,
               $"#id{seen.TgUserId} @{seen.TgUsername}\n" +
               $"/{cq.Data}\n" +
               $"@{ctx.BotUsername}");

            return _users.UpsertSeenAsync(seen, ct);
        }
    }
}

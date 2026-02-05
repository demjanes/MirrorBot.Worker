using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Services.AdminNotifierService;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
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

            var chatId = cq.Message?.Chat.Id ?? cq.From.Id;

            // Чтобы "часики" не крутились
            await client.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

            switch (cb.Action.ToLowerInvariant())
            {
                case "add":
                    await client.SendMessage(chatId, BotUi.Text.AskBotToken, cancellationToken: ct);
                    return;

                case "my":
                    await SendMyBotsPageAsync(ctx, client, chatId, cq, ct);
                    return;

                case "edit":
                    if (!TryGetObjectId(cb.Args, 0, out var editId)) return;
                    await SendBotEditPageAsync(ctx, client, chatId, editId, ct);
                    return;

                case "start":
                    if (!TryGetObjectId(cb.Args, 0, out var startId)) return;
                    await SetBotEnabledAsync(ctx, client, chatId, startId, isEnabled: true, ct);
                    return;

                case "stop":
                    if (!TryGetObjectId(cb.Args, 0, out var stopId)) return;
                    await SetBotEnabledAsync(ctx, client, chatId, stopId, isEnabled: false, ct);
                    return;

                case "del":
                    if (!TryGetObjectId(cb.Args, 0, out var delId)) return;
                    await client.SendMessage(
                        chatId,
                        text: "Удалить бота? Это действие нельзя отменить.",
                        replyMarkup: BotUi.Keyboards.ConfirmDelete(delId.ToString()),
                        cancellationToken: ct);
                    return;

                case "del_yes":
                    if (!TryGetObjectId(cb.Args, 0, out var yesId)) return;
                    await DeleteBotAsync(ctx, client, chatId, yesId, ct);
                    return;

                case "del_no":
                    if (!TryGetObjectId(cb.Args, 0, out var noId)) return;
                    await SendBotEditPageAsync(ctx, client, chatId, noId, ct);
                    return;

                default:
                    await client.AnswerCallbackQuery(cq.Id, BotUi.Text.CallbackUnknown, cancellationToken: ct);
                    return;
            }
        }

        private async Task SendMyBotsPageAsync(BotContext ctx, ITelegramBotClient client, long chatId, CallbackQuery cq, CancellationToken ct)
        {
            // Владелец = тот, кто нажал кнопку (cq.From.Id)
            var ownerId = cq.From.Id;

            var bots = await _mirrorBots.GetByOwnerTgIdAsync(ownerId, ct); // <-- подгони под свой репо
            var items = bots
                .Select(b => new BotListItem(
                    Id: b.Id.ToString(),
                    Title: "@" + (b.BotUsername ?? "unknown"),
                    IsEnabled: b.IsEnabled))
                .ToList();

            await client.SendMessage(
                chatId,
                text: items.Count == 0 ? "У вас пока нет ботов." : "Ваши боты:",
                replyMarkup: BotUi.Keyboards.MyBots(items),
                cancellationToken: ct);
        }

        private async Task SendBotEditPageAsync(BotContext ctx, ITelegramBotClient client, long chatId, ObjectId botId, CancellationToken ct)
        {
            var bot = await _mirrorBots.GetByOdjectIdAsync(botId, ct); // <-- подгони под свой репо
            if (bot is null)
            {
                await client.SendMessage(chatId, "Бот не найден.", cancellationToken: ct);
                return;
            }

            // Безопасность: не даём редактировать чужие боты
            // (если у тебя OwnerTelegramUserId хранится)
            // if (bot.OwnerTelegramUserId != ... ) return;

            var text = $"Бот @{bot.BotUsername}\nСостояние: {(bot.IsEnabled ? "включён" : "выключен")}";
            await client.SendMessage(
                chatId,
                text: text,
                replyMarkup: BotUi.Keyboards.BotEdit(bot.Id.ToString(), bot.IsEnabled),
                cancellationToken: ct);
        }

        private async Task SetBotEnabledAsync(BotContext ctx, ITelegramBotClient client, long chatId, ObjectId botId, bool isEnabled, CancellationToken ct)
        {
            var bot = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
            if (bot is null)
            {
                await client.SendMessage(chatId, "Бот не найден.", cancellationToken: ct);
                return;
            }

            // TODO: security check owner

            var nowUtc = DateTime.UtcNow;
            await _mirrorBots.SetEnabledAsync(botId, isEnabled, nowUtc, ct); // <-- подгони под свой репо

            await client.SendMessage(
                chatId,
                text: isEnabled ? "Бот запущен." : "Бот остановлен.",
                replyMarkup: BotUi.Keyboards.BotEdit(botId.ToString(), isEnabled),
                cancellationToken: ct);
        }

        private async Task DeleteBotAsync(BotContext ctx, ITelegramBotClient client, long chatId, ObjectId botId, CancellationToken ct)
        {
            var bot = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
            if (bot is null)
            {
                await client.SendMessage(chatId, "Бот не найден.", cancellationToken: ct);
                return;
            }

            // TODO: security check owner

            await _mirrorBots.DeleteByOdjectIdAsync(botId, ct); // <-- подгони под свой репо

            await client.SendMessage(chatId, "Бот удалён.", cancellationToken: ct);

            // Перекидываем назад в список
            // (можно вызвать SendMyBotsPageAsync, но нужен CallbackQuery; проще отправить кнопку "Мои боты")
            await client.SendMessage(
                chatId,
                "Открыть список:",
                replyMarkup: new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Мои боты", CbCodec.Pack("bot", "my"))),
                cancellationToken: ct);
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

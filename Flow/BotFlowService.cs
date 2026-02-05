using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data;
using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Services.AdminNotifierService;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MirrorBot.Worker.Flow
{
    public sealed class BotFlowService
    {
        private readonly MirrorBotsRepository _mirrorBots;
        private readonly UsersRepository _users;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TelegramAdminNotifier _notifier;

        public BotFlowService(
            MirrorBotsRepository mirrorBots,
            UsersRepository users,
            IHttpClientFactory httpClientFactory,
            TelegramAdminNotifier notifier
           )
        {
            _mirrorBots = mirrorBots;
            _users = users;
            _httpClientFactory = httpClientFactory;
            _notifier = notifier;
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, Update update, CancellationToken ct)
        {
            // 1) Message
            if (update.Message is { } msg)
            {
                await HandleMessageAsync(ctx, client, msg, ct);
                return;
            }

            // 2) CallbackQuery
            if (update.CallbackQuery is { } cq)
            {
                await HandleCallbackAsync(ctx, client, cq, ct);
                return;
            }

            // остальные типы апдейтов пока игнорируем
        }

        private async Task HandleMessageAsync(BotContext ctx, ITelegramBotClient client, Message msg, CancellationToken ct)
        {
            if (msg.From is null) return;
            if (msg.Text is null) return;

            var nowUtc = DateTime.UtcNow;
            var lastBotKey = GetLastBotKey(ctx);

            // реферал только для зеркал, и нельзя сам себе
            var (refOwner, refBotId) = ComputeReferral(ctx, msg.From.Id);

            var seen = new UserSeenEvent(
                TgUserId: msg.From.Id,
                TgUsername: msg.From.Username,
                TgFirstName: msg.From.FirstName,
                TgLastName: msg.From.LastName,
                LastBotKey: lastBotKey,
                LastChatId: msg.Chat.Id,
                SeenAtUtc: nowUtc,
                ReferrerOwnerTgUserId: refOwner,
                ReferrerMirrorBotId: refBotId
            );

            await _users.UpsertSeenAsync(seen, ct);

            var cmd = CommandRouter.TryGetCommand(msg.Text);

            //_notifier.TryEnqueue(AdminChannel.Info,
            //    $"{DateTime.UtcNow:HH:mm:ss}\n" +
            //    $"#id{seen.TgUserId} @{seen.TgUsername}\n" +
            //    $"text={msg.Text}\n" +
            //    $"bot={lastBotKey}");

            if (cmd == "/start")
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: $"Привет! Владелец этого зеркала: {ctx.OwnerTelegramUserId}",
                    cancellationToken: ct);
                return;
            }

            if (cmd == "/addbot")
            {
                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: "Пришлите токен бота следующим сообщением.",
                    cancellationToken: ct);
                return;
            }

            // добавление зеркала по токену
            if (LooksLikeToken(msg.Text))
            {
                var existing = await _mirrorBots.GetByTokenAsync(msg.Text, ct);
                if (existing is not null)
                {
                    await client.SendMessage(msg.Chat.Id, "Этот токен уже добавлен.", cancellationToken: ct);
                    return;
                }

                var http = _httpClientFactory.CreateClient("telegram");
                var probe = new TelegramBotClient(new TelegramBotClientOptions(msg.Text), http);

                var me = await probe.GetMe(ct);

                await _mirrorBots.InsertAsync(new MirrorBotEntity
                {
                    OwnerTelegramUserId = msg.From.Id,
                    Token = msg.Text,
                    BotUsername = me.Username,
                    IsEnabled = true
                }, ct);

                await client.SendMessage(msg.Chat.Id, $"Зеркало @{me.Username} добавлено. Оно запустится автоматически.", cancellationToken: ct);
                return;
            }

            await client.SendMessage(msg.Chat.Id, "Не понял. /start /addbot", cancellationToken: ct);
        }

        private async Task HandleCallbackAsync(BotContext ctx, ITelegramBotClient client, CallbackQuery cq, CancellationToken ct)
        {
            if (cq.From is null) return;

            // В большинстве сценариев callback приходит из сообщения с inline-клавиатурой, тогда chatId берём отсюда.
            // Если вдруг cq.Message == null (inline message), то для твоего кейса "только личные чаты" можно fallback на From.Id.
            var chatId = cq.Message?.Chat.Id ?? cq.From.Id;

            var nowUtc = DateTime.UtcNow;
            var lastBotKey = GetLastBotKey(ctx);

            var (refOwner, refBotId) = ComputeReferral(ctx, cq.From.Id);

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

            await _users.UpsertSeenAsync(seen, ct);

            // Всегда "закрываем" callback, чтобы Telegram показал реакцию на нажатие
            await client.AnswerCallbackQuery(
                callbackQueryId: cq.Id,
                text: "Ok",
                cancellationToken: ct);

            // Пример логики по данным кнопки
            var data = cq.Data ?? string.Empty;

            if (data == "ping")
            {
                await client.SendMessage(chatId, "pong", cancellationToken: ct);
                return;
            }

            await client.SendMessage(chatId, $"Нажата кнопка: {data}", cancellationToken: ct);
        }

        private static string GetLastBotKey(BotContext ctx)
            => ctx.MirrorBotId == ObjectId.Empty ? "__main__" : ctx.MirrorBotId.ToString();

        private static (long? RefOwner, ObjectId? RefBotId) ComputeReferral(BotContext ctx, long currentUserId)
        {
            if (ctx.OwnerTelegramUserId == 0) return (null, null);
            if (ctx.MirrorBotId == ObjectId.Empty) return (null, null);

            // нельзя быть самому себе рефералом
            if (currentUserId == ctx.OwnerTelegramUserId) return (null, null);

            return (ctx.OwnerTelegramUserId, ctx.MirrorBotId);
        }

        private static bool LooksLikeToken(string text)
            => text.Contains(':') && text.Length >= 20;
    }
}

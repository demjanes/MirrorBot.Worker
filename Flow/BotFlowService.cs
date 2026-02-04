using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data;
using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
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

        public BotFlowService(
            MirrorBotsRepository mirrorBots,
            UsersRepository users,
            IHttpClientFactory httpClientFactory)
        {
            _mirrorBots = mirrorBots;
            _users = users;
            _httpClientFactory = httpClientFactory;
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, Update update, CancellationToken ct)
        {
            if (update.Message is not { } msg) return;
            if (msg.From is null) return;
            if (msg.Text is null) return;

            var nowUtc = DateTime.UtcNow;

            var lastBotKey = ctx.MirrorBotId == MongoDB.Bson.ObjectId.Empty
                ? "__main__"
                : ctx.MirrorBotId.ToString();

            // реферал только для зеркал
            long? refOwner = null;
            MongoDB.Bson.ObjectId? refBotId = null;

            if (ctx.OwnerTelegramUserId != 0 && ctx.MirrorBotId != ObjectId.Empty)
            {
                if (msg.From.Id != ctx.OwnerTelegramUserId)
                {
                    refOwner = ctx.OwnerTelegramUserId;
                    refBotId = ctx.MirrorBotId;
                }
            }

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

            // 2) команды
            var cmd = CommandRouter.TryGetCommand(msg.Text);

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

            //добавление бота
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

        private static bool LooksLikeToken(string text)
            => text.Contains(':') && text.Length >= 20; // грубо, потом улучшишь
    }
}

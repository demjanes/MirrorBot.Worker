using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data;
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

            // 1) user upsert-lite
            var u = await _users.GetByTelegramIdAsync(msg.From.Id, ct);
            if (u is null)
            {
                await _users.InsertAsync(new UserEntity
                {
                    TelegramUserId = msg.From.Id,
                    Username = msg.From.Username
                }, ct);

                await _users.SetReferralIfEmptyAsync(msg.From.Id, ctx.OwnerTelegramUserId, ctx.MirrorBotId, ct);
            }

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

            // Упрощение: если сообщение похоже на токен — считаем, что это токен после /addbot
            if (LooksLikeToken(msg.Text))
            {
                // Проверка на дубль
                var existing = await _mirrorBots.GetByTokenAsync(msg.Text, ct);
                if (existing is not null)
                {
                    await client.SendMessage(
                        chatId: msg.Chat.Id,
                        text: "Этот токен уже добавлен.",
                        cancellationToken: ct);

                    return;
                }

                // Валидация токена через getMe
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

                await client.SendMessage(
                    chatId: msg.Chat.Id,
                    text: $"Зеркало @{me.Username} добавлено. Оно запустится автоматически.",
                    cancellationToken: ct);

                return;
            }

            await client.SendMessage(
                chatId: msg.Chat.Id,
                text: "Не понял. /start /addbot",
                cancellationToken: ct);
        }

        private static bool LooksLikeToken(string text)
            => text.Contains(':') && text.Length >= 20; // грубо, потом улучшишь
    }
}

using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Entities;
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

namespace MirrorBot.Worker.Flow.Handlers
{
    public sealed class BotMessageHandler
    {
        private readonly UsersRepository _users;
        private readonly MirrorBotsRepository _mirrorBots;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAdminNotifier _notifier;

        private readonly IReadOnlyDictionary<string, Func<BotContext, ITelegramBotClient, Message, CancellationToken, Task>> _commands;

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

            _commands = new Dictionary<string, Func<BotContext, ITelegramBotClient, Message, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                [BotRoutes.Commands.Start] = (ctx, client, msg, ct) =>
                    client.SendMessage(
                        chatId: msg.Chat.Id,
                        text: BotUi.Text.Start(ctx.OwnerTelegramUserId),
                        replyMarkup: BotUi.Keyboards.MainMenu(),
                        cancellationToken: ct),

                [BotRoutes.Commands.AddBot] = (ctx, client, msg, ct) =>
                    client.SendMessage(
                        chatId: msg.Chat.Id,
                        text: BotUi.Text.AskBotToken,
                        cancellationToken: ct),
            };
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, Message msg, CancellationToken ct)
        {
            if (msg.From is null) return;

            await UpsertSeenFromMessageAsync(ctx, msg, ct);

            if (msg.Text is null) return;

            // 1) команды
            var cmd = TryGetCommand(msg.Text);
            if (cmd is not null && _commands.TryGetValue(cmd, out var action))
            {
                await action(ctx, client, msg, ct);
                return;
            }

            // 2) ввод токена (как раньше)
            if (LooksLikeToken(msg.Text))
            {
                await TryAddMirrorBotByTokenAsync(client, msg, ct);
                return;
            }

            // 3) дефолт — подсказка + меню
            await client.SendMessage(
                chatId: msg.Chat.Id,
                text: BotUi.Text.Unknown,
                replyMarkup: BotUi.Keyboards.MainMenu(),
                cancellationToken: ct);                       
        }


        // --- Internals ---
        private async Task TryAddMirrorBotByTokenAsync(ITelegramBotClient client, Message msg, CancellationToken ct)
        {
            var token = msg.Text!;
            var existing = await _mirrorBots.GetByTokenAsync(token, ct);
            if (existing is not null)
            {
                await client.SendMessage(msg.Chat.Id, BotUi.Text.TokenAlreadyAdded, cancellationToken: ct);
                return;
            }

            var http = _httpClientFactory.CreateClient("telegram");
            var probe = new TelegramBotClient(new TelegramBotClientOptions(token), http);
            var me = await probe.GetMe(ct);

            await _mirrorBots.InsertAsync(new MirrorBotEntity
            {
                OwnerTelegramUserId = msg.From!.Id,
                Token = token,
                BotUsername = me.Username,
                IsEnabled = true
            }, ct);

            // После добавления — сразу меню "Мои боты"
            await client.SendMessage(
                chatId: msg.Chat.Id,
                text: BotUi.Text.MirrorAdded(me.Username!),
                replyMarkup: new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Мои боты", CbCodec.Pack("bot", "my"))),
                cancellationToken: ct);
        }

        private Task UpsertSeenFromMessageAsync(BotContext ctx, Message msg, CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;
            var lastBotKey = ctx.MirrorBotId == ObjectId.Empty ? "__main__" : ctx.MirrorBotId.ToString();

            long? refOwner = null;
            ObjectId? refBotId = null;

            // реферал только для зеркал + нельзя сам себе
            if (ctx.OwnerTelegramUserId != 0 && ctx.MirrorBotId != ObjectId.Empty && msg.From!.Id != ctx.OwnerTelegramUserId)
            {
                refOwner = ctx.OwnerTelegramUserId;
                refBotId = ctx.MirrorBotId;
            }

            var seen = new UserSeenEvent(
                TgUserId: msg.From!.Id,
                TgUsername: msg.From.Username,
                TgFirstName: msg.From.FirstName,
                TgLastName: msg.From.LastName,
                LastBotKey: lastBotKey,
                LastChatId: msg.Chat.Id,
                SeenAtUtc: nowUtc,
                ReferrerOwnerTgUserId: refOwner,
                ReferrerMirrorBotId: refBotId
            );

            _notifier.TryEnqueue(AdminChannel.Info,
                $"#id{seen.TgUserId} @{seen.TgUsername}\n" +
                $"{msg.Text}\n" +
                $"@{ctx.BotUsername}");


            return _users.UpsertSeenAsync(seen, ct);
        }

        private static string? TryGetCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!text.StartsWith('/')) return null;

            var first = text.Split(' ', '\n', '\t')[0];
            return first.Split('@')[0];
        }

        private static bool LooksLikeToken(string text)
            => text.Contains(':') && text.Length >= 20;
    }
}

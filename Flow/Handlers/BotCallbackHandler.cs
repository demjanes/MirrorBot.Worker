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

namespace MirrorBot.Worker.Flow.Handlers
{
    public sealed class BotCallbackHandler
    {
        private readonly UsersRepository _users;
        private readonly IAdminNotifier _notifier;
        
        private readonly IReadOnlyDictionary<string, Func<BotContext, ITelegramBotClient, CallbackQuery, CancellationToken, Task>> _callbacks;

        public BotCallbackHandler(
            UsersRepository users, 
            IAdminNotifier notifier)
        {
            _users = users;
            _notifier = notifier;

            _callbacks = new Dictionary<string, Func<BotContext, ITelegramBotClient, CallbackQuery, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                [BotRoutes.Callbacks.Ping] = async (ctx, client, cq, ct) =>
                {
                    await client.AnswerCallbackQuery(cq.Id, "pong", cancellationToken: ct);
                    var chatId = cq.Message?.Chat.Id ?? cq.From.Id;
                    await client.SendMessage(chatId, "pong", cancellationToken: ct);
                }
            };
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, CallbackQuery cq, CancellationToken ct)
        {
            await UpsertSeenFromCallbackAsync(ctx, cq, ct);

            var key = cq.Data;
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (_callbacks.TryGetValue(key, out var action))
            {
                await action(ctx, client, cq, ct);
                return;
            }

            await client.AnswerCallbackQuery(cq.Id, BotUi.Text.CallbackUnknown, cancellationToken: ct);
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

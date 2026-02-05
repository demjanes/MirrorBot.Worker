using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Flow.Handlers;
using MirrorBot.Worker.Services.AdminNotifierService;
using MongoDB.Bson;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MirrorBot.Worker.Flow
{
    public sealed class BotFlowService
    {
        private readonly BotMessageHandler _messages;
        private readonly BotCallbackHandler _callbacks;

        public BotFlowService(BotMessageHandler messages, BotCallbackHandler callbacks)
        {
            _messages = messages;
            _callbacks = callbacks;
        }

        public Task HandleAsync(BotContext ctx, ITelegramBotClient client, Update update, CancellationToken ct)
        {
            if (update.Message is { } msg)
                return _messages.HandleAsync(ctx, client, msg, ct);

            if (update.CallbackQuery is { } cq)
                return _callbacks.HandleAsync(ctx, client, cq, ct);

            return Task.CompletedTask;
        }
    }
}

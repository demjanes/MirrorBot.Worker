using MirrorBot.Worker.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace MirrorBot.Worker.Bot
{
    public sealed class BotUpdateHandler : IUpdateHandler
    {
        private readonly BotContext _ctx;
        private readonly BotFlowService _flow;
        private readonly ILogger<BotUpdateHandler> _log;

        public BotUpdateHandler(BotContext ctx, BotFlowService flow, ILogger<BotUpdateHandler> log)
        {
            _ctx = ctx;
            _flow = flow;
            _log = log;
        }

        public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
            => _flow.HandleAsync(_ctx, botClient, update, ct);

        public async Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            HandleErrorSource source,
            CancellationToken ct)
        {
            _log.LogWarning(exception,
                "Polling error. source={Source} botId={BotId} username={Username} owner={Owner}",
                source, _ctx.MirrorBotId, _ctx.BotUsername, _ctx.OwnerTelegramUserId);

            // При сетевых сбоях polling может часто падать, задержка снижает нагрузку [web:26]
            await Task.Delay(TimeSpan.FromSeconds(1), ct);

            // Если захочешь "падать" при ошибках твоего кода:
            // if (source == HandleErrorSource.HandleUpdateError) throw exception; // идея из миграции [web:97]
        }
    }
}

using MirrorBot.Worker.Flow;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace MirrorBot.Worker.Bot
{
    public sealed class BotUpdateHandler : IUpdateHandler
    {
        private readonly BotContext _ctx;
        private readonly IServiceScopeFactory _scopeFactory; // ✅ ИЗМЕНЕНО
        private readonly ILogger<BotUpdateHandler> _log;

        public BotUpdateHandler(
            BotContext ctx,
            IServiceScopeFactory scopeFactory, // ✅ ИЗМЕНЕНО
            ILogger<BotUpdateHandler> log)
        {
            _ctx = ctx;
            _scopeFactory = scopeFactory; // ✅ ИЗМЕНЕНО
            _log = log;
        }

        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken ct)
        {
            // ✅ ДОБАВЛЕНО: Создаем scope для каждого обновления
            using var scope = _scopeFactory.CreateScope();
            var flow = scope.ServiceProvider.GetRequiredService<BotFlowService>();

            await flow.HandleAsync(_ctx, botClient, update, ct); // ✅ ПРАВИЛЬНОЕ имя метода
        }

        public async Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            HandleErrorSource source,
            CancellationToken ct)
        {
            _log.LogWarning(exception,
                "Polling error. source={Source} botId={BotId} username={Username} owner={Owner}",
                source, _ctx.MirrorBotId, _ctx.BotUsername, _ctx.OwnerTelegramUserId);

            // При сетевых сбоях polling может часто падать, задержка снижает нагрузку
            await Task.Delay(TimeSpan.FromSeconds(1), ct);

            // Если захочешь "падать" при ошибках твоего кода:
            // if (source == HandleErrorSource.HandleUpdateError) throw exception;
        }
    }
}

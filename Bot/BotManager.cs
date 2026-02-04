using Microsoft.Extensions.Options;
using MirrorBot.Worker.Data;
using MirrorBot.Worker.Flow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MirrorBot.Worker.Bot
{
    public sealed class BotManager : BackgroundService
    {
        private const string MainKey = "__main__";

        private readonly ILogger<BotManager> _log;
        private readonly MirrorBotsRepository _repo;
        private readonly BotFlowService _flow;
        private readonly IOptions<BotConfiguration> _mainOpt;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerFactory _loggerFactory;

        private readonly ConcurrentDictionary<string, BotRunner> _runners = new();

        public BotManager(
            ILogger<BotManager> log,
            MirrorBotsRepository repo,
            BotFlowService flow,
            IOptions<BotConfiguration> mainOpt,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory)
        {
            _log = log;
            _repo = repo;
            _flow = flow;
            _mainOpt = mainOpt;
            _httpClientFactory = httpClientFactory;
            _loggerFactory = loggerFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            StartMainBot();

            while (!stoppingToken.IsCancellationRequested)
            {
                var enabled = await _repo.GetEnabledAsync(stoppingToken);
                var enabledKeys = new HashSet<string>(enabled.Select(x => x.Id.ToString()));

                // старт новых
                foreach (var b in enabled)
                {
                    var key = b.Id.ToString();
                    if (_runners.ContainsKey(key)) continue;

                    TryStartMirror(key, new BotContext(b.Id, b.OwnerTelegramUserId, b.Token, b.BotUsername));
                }

                // стоп отключённых/удалённых
                foreach (var kv in _runners)
                {
                    var key = kv.Key;
                    if (key == MainKey) continue;
                    if (enabledKeys.Contains(key)) continue;

                    if (_runners.TryRemove(key, out var runner))
                    {
                        runner.Stop();
                        _log.LogInformation("Stopped bot runner {BotKey}", key);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var kv in _runners)
            {
                kv.Value.Stop();
            }
            return base.StopAsync(cancellationToken);
        }

        private void StartMainBot()
        {
            var ctx = new BotContext(
                MirrorBotId: MongoDB.Bson.ObjectId.Empty,
                OwnerTelegramUserId: 0,
                Token: _mainOpt.Value.BotToken,
                BotUsername: "main"
            );

            if (_runners.ContainsKey(MainKey)) return;
            TryStartMirror(MainKey, ctx);
        }

        private void TryStartMirror(string key, BotContext ctx)
        {
            // дедуп: TryAdd -> только победитель стартует
            var http = _httpClientFactory.CreateClient("telegram");
            var client = new TelegramBotClient(new TelegramBotClientOptions(ctx.Token), http); // можно так, конструктор поддерживается [web:59]

            var handler = new BotUpdateHandler(ctx, _flow, _loggerFactory.CreateLogger<BotUpdateHandler>());
            var runner = new BotRunner(ctx, client, handler);

            if (!_runners.TryAdd(key, runner))
                return;

            try
            {
                runner.Start(); // внутри StartReceiving(...) [web:19]
                _log.LogInformation("Started bot runner {BotKey} @{Username}", key, ctx.BotUsername);
            }
            catch
            {
                _runners.TryRemove(key, out _);
                runner.Stop();
                throw;
            }
        }
    }
}

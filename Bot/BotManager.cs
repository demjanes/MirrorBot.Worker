using Microsoft.Extensions.Options;
using MirrorBot.Worker.Configs;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Flow;
using MirrorBot.Worker.Services.TokenEncryption;
using System.Collections.Concurrent;
using Telegram.Bot;

namespace MirrorBot.Worker.Bot
{
    public sealed class BotManager : BackgroundService, IBotClientResolver
    {
        public const string MainKey = "__main__";

        private readonly ILogger<BotManager> _log;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<BotConfiguration> _mainOpt;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenEncryptionService _tokenEncryption;

        private readonly ConcurrentDictionary<string, BotRunner> _runners = new();

        public BotManager(
            ILoggerFactory loggerFactory,
            ILogger<BotManager> log,
            IServiceScopeFactory scopeFactory,
            IOptions<BotConfiguration> mainOpt,
            IHttpClientFactory httpClientFactory,
            ITokenEncryptionService tokenEncryption)
        {
            _loggerFactory = loggerFactory;
            _log = log;
            _scopeFactory = scopeFactory;
            _mainOpt = mainOpt;
            _httpClientFactory = httpClientFactory;
            _tokenEncryption = tokenEncryption;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await StartMainBotAsync(stoppingToken);  // ✅ ИЗМЕНЕНО: сделал async для GetMe

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IMirrorBotsRepository>();

                        var enabled = await repo.GetEnabledAsync(stoppingToken);

                        var enabledKeys = new HashSet<string>(enabled.Select(x => x.Id.ToString()));

                        // старт новых
                        foreach (var b in enabled)
                        {
                            var key = b.Id.ToString();
                            if (_runners.ContainsKey(key)) continue;

                            await TryStartMirrorAsync(key, new BotContext(b.Id, b.OwnerTelegramUserId, b.EncryptedToken, b.BotUsername), stoppingToken);  // ✅ ИЗМЕНЕНО
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
                            }
                        }

                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        _log.LogError(ex, "Ошибка в цикле мониторинга зеркал");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogCritical(ex, "Критическая ошибка в BotManager.ExecuteAsync");
                throw;
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

        private async Task StartMainBotAsync(CancellationToken cancellationToken)  // ✅ ИЗМЕНЕНО: async
        {
            var ctx = new BotContext(
                MirrorBotId: MongoDB.Bson.ObjectId.Empty,
                OwnerTelegramUserId: 0,
                Token: _mainOpt.Value.BotToken,
                BotUsername: "main"
            );

            if (_runners.ContainsKey(MainKey))
            {
                return;
            }

            await TryStartMirrorAsync(MainKey, ctx, cancellationToken);  // ✅ ИЗМЕНЕНО
        }

        private async Task TryStartMirrorAsync(string key, BotContext ctx, CancellationToken cancellationToken)  // ✅ ИЗМЕНЕНО: async
        {
            var http = _httpClientFactory.CreateClient("telegram");

            string plainToken;

            // Проверяем, зашифрован ли токен или это открытый токен основного бота
            if (IsEncryptedToken(ctx.Token))
            {
                // Это зеркало с зашифрованным токеном
                try
                {
                    plainToken = _tokenEncryption.Decrypt(ctx.Token);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to decrypt token for bot {BotKey}", key);
                    return;
                }
            }
            else
            {
                // Это основной бот с открытым токеном из конфига
                plainToken = ctx.Token;
            }

            var client = new TelegramBotClient(new TelegramBotClientOptions(plainToken), http);

            // ✅ ДОБАВЛЕНО: Получаем реальный username бота через GetMe
            try
            {
                var botInfo = await client.GetMe(cancellationToken);

                // Обновляем контекст с реальным username
                ctx = new BotContext(
                    ctx.MirrorBotId,
                    ctx.OwnerTelegramUserId,
                    ctx.Token,
                    botInfo.Username ?? ctx.BotUsername);

                _log.LogInformation("Bot @{Username} initialized (key: {BotKey})", ctx.BotUsername, key);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to get bot info for {BotKey}", key);
                return;
            }

            var handler = new BotUpdateHandler(
                ctx,
                _scopeFactory,
                _loggerFactory.CreateLogger<BotUpdateHandler>());

            var runner = new BotRunner(ctx, client, handler);

            if (!_runners.TryAdd(key, runner))
            {
                return;
            }

            try
            {
                runner.Start();
                _log.LogInformation("Started bot runner {BotKey} @{Username}", key, ctx.BotUsername);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Ошибка при запуске бота {BotKey}", key);
                _runners.TryRemove(key, out _);
                runner.Stop();
                throw;
            }
        }

        /// <summary>
        /// Проверяет, похож ли токен на зашифрованный (Base64 с бинарными данными)
        /// или это открытый токен формата "123456789:ABC..."
        /// </summary>
        private static bool IsEncryptedToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            // Открытый токен Telegram всегда имеет формат: цифры:буквы/цифры
            // Пример: 123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefgh
            if (token.Contains(':') && char.IsDigit(token[0]))
            {
                // Это похоже на открытый токен
                return false;
            }

            // Пытаемся распарсить как Base64
            try
            {
                Convert.FromBase64String(token);

                // Если успешно распарсили и токен не содержит двоеточия,
                // то это вероятно зашифрованный токен
                return !token.Contains(':');
            }
            catch
            {
                // Если не парсится как Base64, то это открытый токен
                return false;
            }
        }

        public bool TryGetClient(string botKey, out ITelegramBotClient client)
        {
            client = default!;
            if (!_runners.TryGetValue(botKey, out var runner))
                return false;

            client = runner.Client;
            return true;
        }

        /// <summary>
        /// Получить клиент бота по username.
        /// </summary>
        public ITelegramBotClient? ResolveBotByUsername(string botUsername)
        {
            if (string.IsNullOrWhiteSpace(botUsername))
                return null;

            // Ищем среди запущенных ботов
            var runner = _runners.Values.FirstOrDefault(
                r => r.Context.BotUsername?.Equals(botUsername, StringComparison.OrdinalIgnoreCase) == true);

            return runner?.Client;
        }
    }
}

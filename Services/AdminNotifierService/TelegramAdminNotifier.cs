using Microsoft.Extensions.Options;
using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Configs;
using System.Threading.Channels;
using Telegram.Bot;

namespace MirrorBot.Worker.Services.AdminNotifierService
{
    public sealed class TelegramAdminNotifier : BackgroundService, IAdminNotifier
    {
        private readonly ITelegramBotClient _adminBot;
        private readonly IOptionsMonitor<AdminNotificationsConfiguration> _opt;
        private readonly ILogger<TelegramAdminNotifier> _log;

        private readonly Channel<(AdminChannel Channel, string Text)> _queue;

        public TelegramAdminNotifier(
            ITelegramBotClient adminBot,               // это клиент MAIN бота
            IOptionsMonitor<AdminNotificationsConfiguration> opt,
            ILogger<TelegramAdminNotifier> log)
        {
            _adminBot = adminBot;
            _opt = opt;
            _log = log;

            _queue = Channel.CreateBounded<(AdminChannel, string)>(new BoundedChannelOptions(_opt.CurrentValue.MaxQueueSize)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }

        public bool TryEnqueue(AdminChannel channel, string text)
        {
            var o = _opt.CurrentValue;
            if (!o.Enabled) return false;
            if (channel == AdminChannel.Info && !o.Info.Enabled) return false;
            if (channel == AdminChannel.Ref && !o.Ref.Enabled) return false;

            return _queue.Writer.TryWrite((channel, text));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var o = _opt.CurrentValue;

                // ждём либо первый элемент, либо таймер
                var delayTask = Task.Delay(o.FlushIntervalMs, stoppingToken);
                var readTask = _queue.Reader.WaitToReadAsync(stoppingToken).AsTask();
                await Task.WhenAny(delayTask, readTask);

                await FlushAsync(stoppingToken);
            }
        }

        private async Task FlushAsync(CancellationToken ct)
        {
            var o = _opt.CurrentValue;
            if (!o.Enabled) return;

            var items = new List<(AdminChannel Channel, string Text)>(capacity: Math.Max(1, o.BatchSize));

            while (items.Count < o.BatchSize && _queue.Reader.TryRead(out var item))
                items.Add(item);

            if (items.Count == 0) return;

            // группируем по каналу
            foreach (var grp in items.GroupBy(x => x.Channel))
            {
                var chatId = grp.Key switch
                {
                    AdminChannel.Info => o.Info.ChatId,
                    AdminChannel.Ref => o.Ref.ChatId,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var texts = grp.Select(x => x.Text).ToArray();

                try
                {
                    if (o.CombineIntoSingleMessage)
                    {
                        var combined = string.Join("\n\n", texts);
                        await _adminBot.SendMessage(chatId, combined, cancellationToken: ct); // v22
                    }
                    else
                    {
                        foreach (var t in texts)
                            await _adminBot.SendMessage(chatId, t, cancellationToken: ct); // v22
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to send admin notifications to {Channel}", grp.Key);
                }
            }
        }
    }
}

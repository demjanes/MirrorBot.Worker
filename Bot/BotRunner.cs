using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace MirrorBot.Worker.Bot
{
    public sealed class BotRunner
    {
        public BotContext Context { get; }
        public ITelegramBotClient Client => _client;

        private readonly ITelegramBotClient _client;
        private readonly IUpdateHandler _handler;
        private CancellationTokenSource? _cts;

        public BotRunner(BotContext context, ITelegramBotClient client, IUpdateHandler handler)
        {
            Context = context;
            _client = client;
            _handler = handler;
        }

        public void Start()
        {
            if (_cts is not null) return; // уже запущен

            _cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
                // ThrowPendingUpdates = true // см. пункт 3
            };

            _client.StartReceiving(_handler, receiverOptions, _cts.Token); // polling цикл [web:19]
        }


        public void Stop()
        {
            var cts = Interlocked.Exchange(ref _cts, null);
            if (cts is null) return;
            cts.Cancel();
            cts.Dispose();
        }

    }
}

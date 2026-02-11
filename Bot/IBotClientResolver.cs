using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MirrorBot.Worker.Bot
{
    public interface IBotClientResolver
    {
        bool TryGetClient(string botKey, out ITelegramBotClient client);
        /// <summary>
        /// Получить клиент бота по username.
        /// </summary>
        ITelegramBotClient? ResolveBotByUsername(string botUsername);
    }
}

using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Flow.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MirrorBot.Worker.Flow.Routes
{
    public static class BotRoutes
    {
        public static class Commands
        {
            public const string Start = "/start";
            public const string AddBot = "/addbot";
        }

        public static class Callbacks
        {
            public const string Ping = "ping";
        }
    }
}

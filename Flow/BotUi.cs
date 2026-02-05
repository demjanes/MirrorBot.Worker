using MirrorBot.Worker.Flow.Routes;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow
{
    public static class BotUi
    {
        public static class Text
        {
            public static string Start(long ownerId) => $"Привет! Владелец этого зеркала: {ownerId}";
            public const string AskBotToken = "Пришлите токен бота следующим сообщением.";
            public const string TokenAlreadyAdded = "Этот токен уже добавлен.";
            public static string MirrorAdded(string username) => $"Зеркало @{username} добавлено. Оно запустится автоматически.";
            public const string Unknown = "Не понял. /start /addbot";
            public const string CallbackUnknown = "Неизвестная кнопка";
        }

        public static class Keyboards
        {
            public static InlineKeyboardMarkup MainMenu()
                => new(InlineKeyboardButton.WithCallbackData("Ping", BotRoutes.Callbacks.Ping));
        }
    }
}

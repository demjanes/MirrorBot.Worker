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
            => new(new[]
            {
               new []
               {
                   InlineKeyboardButton.WithCallbackData("Мои боты", CbCodec.Pack("bot", "my")),
                   InlineKeyboardButton.WithCallbackData("Ping", CbCodec.Pack("bot", "ping")) // если хочешь ping через общий формат
               },
               new []
               {
                   InlineKeyboardButton.WithCallbackData("➕ Добавить", CbCodec.Pack("bot", "add"))
               }
            });



            public sealed record BotListItem(string Id, string Title, bool IsEnabled);
            public static InlineKeyboardMarkup MyBots(IReadOnlyList<BotListItem> bots)
            {
                // 1 кнопка = 1 ряд (удобно для списков) [web:516]
                var rows = bots
                    .Select(b =>
                        new[]
                        {
                        InlineKeyboardButton.WithCallbackData(
                            text: $"{(b.IsEnabled ? "🟢" : "🔴")} {b.Title}",
                            callbackData: CbCodec.Pack("bot", "edit", b.Id))
                        })
                    .ToList();

                // нижний ряд действий
                rows.Add(new[]
                {
                InlineKeyboardButton.WithCallbackData("➕ Добавить", CbCodec.Pack("bot", "add")),
                InlineKeyboardButton.WithCallbackData("↻ Обновить", CbCodec.Pack("bot", "my")),
            });

                return new InlineKeyboardMarkup(rows);
            }

            public static InlineKeyboardMarkup BotEdit(string botId, bool isEnabled)
            {
                var startStop = isEnabled
                    ? InlineKeyboardButton.WithCallbackData("⏸ Stop", CbCodec.Pack("bot", "stop", botId))
                    : InlineKeyboardButton.WithCallbackData("▶️ Start", CbCodec.Pack("bot", "start", botId));

                return new InlineKeyboardMarkup(new[]
                {
                new [] { startStop },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("🗑 Удалить", CbCodec.Pack("bot", "del", botId)),
                    InlineKeyboardButton.WithCallbackData("↩️ Мои боты", CbCodec.Pack("bot", "my")),
                }
            });
            }

            public static InlineKeyboardMarkup ConfirmDelete(string botId)
                => new(new[]
                {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("✅ Да, удалить", CbCodec.Pack("bot", "del_yes", botId)),
                    InlineKeyboardButton.WithCallbackData("❌ Нет", CbCodec.Pack("bot", "del_no", botId)),
                },
                new []
                {
                    InlineKeyboardButton.WithCallbackData("↩️ Назад", CbCodec.Pack("bot", "edit", botId)),
                }
                });
        }
    }
}

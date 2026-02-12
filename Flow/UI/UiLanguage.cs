using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI.Models;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.UI
{
    public static class UiLanguage
    {
        public static string Choose(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("Choose language:");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Выберите язык:");
                    break;
            }
            return sb.ToString();
        }

        public static string Set(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("✅ Language changed to English");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("✅ Язык изменён на русский");
                    break;
            }
            return sb.ToString();
        }

        // Keyboards
        public static InlineKeyboardMarkup ChooseKeyboard(BotTask entity)
        {
            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🇷🇺 Русский", BotRoutes.Callbacks.Lang.Set(UiLang.Ru)),
                    InlineKeyboardButton.WithCallbackData("🇬🇧 English", BotRoutes.Callbacks.Lang.Set(UiLang.En)),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }
    }
}

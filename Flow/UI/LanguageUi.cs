using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Flow.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.UI
{
    public static class LanguageUi
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

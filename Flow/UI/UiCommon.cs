using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI.Models;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.UI
{
    public static class UiCommon
    {
        public static string Start(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("Hello! It's start message:");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Привет! Это стартовое сообщение");
                    break;
            }
            return sb.ToString();
        }

        public static string HideKbrd(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("HideKbrd done");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine($"Клавиатура скрыта. Если потребуется повторно введите: {BotRoutes.Commands.Start}");
                    break;
            }
            return sb.ToString();
        }

        public static string Unknown(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("Unknown command");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Неизвестная команда");
                    break;
            }
            return sb.ToString();
        }

        public static string Menu(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("MENU message");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Сообщение МЕНЮ");
                    break;
            }
            return sb.ToString();
        }

        public static string Help(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("HELP message");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Сообщение ПОМОЩЬ");
                    break;
            }
            return sb.ToString();
        }

        // Keyboards
        public static ReplyKeyboardMarkup StartRKeyboard(BotTask entity)
        {
            var kb = new[]
            {
                new[]
                {
                    new KeyboardButton(BotRoutes.Commands.MenuTxt_Ru),
                    new KeyboardButton(BotRoutes.Commands.HelpTxt_Ru),
                },
                new[]
                {
                    new KeyboardButton(BotRoutes.Commands.HideKbrdTxt_Ru),
                }
            };

            return new ReplyKeyboardMarkup(kb)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false,
                InputFieldPlaceholder = "",
                Selective = false
            };
        }

        public static InlineKeyboardMarkup MenuKeyboard(BotTask entity)
        {
            var helpText = entity.AnswerLang == UiLang.En ? "❓ Help" : "❓ Помощь";
            var refText = entity.AnswerLang == UiLang.En ? "👥 Referral" : "👥 Реферальная программа";
            var langText = entity.AnswerLang == UiLang.En ? "🌍 Language" : "🌍 Язык";
            var botsText = entity.AnswerLang == UiLang.En ? "🤖 My Bots" : "🤖 Мои боты";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(helpText, BotRoutes.Callbacks.Menu.Help),
                    InlineKeyboardButton.WithCallbackData(refText, BotRoutes.Callbacks.Menu.Ref),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(langText, BotRoutes.Callbacks.Lang.Choose),
                    InlineKeyboardButton.WithCallbackData(botsText, BotRoutes.Callbacks.Bot.My),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }

        public static InlineKeyboardMarkup HelpKeyboard(BotTask entity)
        {
            var menuText = entity.AnswerLang == UiLang.En ? "📋 Menu" : "📋 Меню";
            var refText = entity.AnswerLang == UiLang.En ? "👥 Referral" : "👥 Реферальная программа";
            var langText = entity.AnswerLang == UiLang.En ? "🌍 Language" : "🌍 Язык";
            var botsText = entity.AnswerLang == UiLang.En ? "🤖 My Bots" : "🤖 Мои боты";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(menuText, BotRoutes.Callbacks.Menu.MenuMain),
                    InlineKeyboardButton.WithCallbackData(refText, BotRoutes.Callbacks.Menu.Ref),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(langText, BotRoutes.Callbacks.Lang.Choose),
                    InlineKeyboardButton.WithCallbackData(botsText, BotRoutes.Callbacks.Bot.My),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }

        // Constants
        public const string AskBotToken = "🤖 Пришлите токен бота.";
        public const string TokenAlreadyAdded = "⚠️ Этот токен уже добавлен.";
        public const string CallbackUnknown = "⚠️ Неизвестная команда";

        public static string MirrorAdded(string username) =>
            $"✅ {username} добавлен. Он будет запущен автоматически.";
    }
}


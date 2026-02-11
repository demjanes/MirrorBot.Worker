using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.UI
{
    public static class BotManagementUi
    {
        public static string MyBots(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("Yours Bots:");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Ваши боты:");
                    break;
            }
            return sb.ToString();
        }

        public static string Add(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("Adding Bots:");
                    sb.AppendLine("Send Me Token of Bot");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Добавление бота:");
                    sb.AppendLine("Пришлите мне токен бота.");
                    break;
            }
            return sb.ToString();
        }

        public static string AddResult(BotTask entity)
        {
            var bot = entity.BotMirror;
            if (bot is null) return string.Empty;

            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine($"Mirror @{bot.BotUsername} added. It will be start automatically.");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine($"Бот @{bot.BotUsername} добавлен. Он будет запущен автоматически.");
                    break;
            }
            return sb.ToString();
        }

        public static string Edit(BotTask entity)
        {
            var bot = entity.BotMirror;
            if (bot is null) return string.Empty;

            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine($"Bot @{bot.BotUsername}");
                    sb.AppendLine($"Status: {(bot.IsEnabled ? "✅ ON" : "❌ OFF")}.");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine($"Бот @{bot.BotUsername}");
                    sb.AppendLine($"Статус: {(bot.IsEnabled ? "✅ Включен" : "❌ Выключен")}.");
                    break;
            }
            return sb.ToString();
        }

        public static string EditNotFound(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("Bot is not found.");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Бот не найден.");
                    break;
            }
            return sb.ToString();
        }

        public static string EditNoAccess(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("No access to bot.");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Нет доступа к боту.");
                    break;
            }
            return sb.ToString();
        }

        public static string DeleteConfirm(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("Delete bot??");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("Удалить бота?");
                    break;
            }
            return sb.ToString();
        }

        public static string DeleteYesResult(BotTask entity)
        {
            var bot = entity.BotMirror;
            if (bot is null) return string.Empty;

            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine($"Bot @{bot.BotUsername} delete.");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine($"Бот @{bot.BotUsername} удалён.");
                    break;
            }
            return sb.ToString();
        }

        // Keyboards
        public static InlineKeyboardMarkup AddResultKeyboard(BotTask entity)
        {
            var botsText = entity.AnswerLang == UiLang.En ? "🤖 My Bots" : "🤖 Мои боты";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(botsText, BotRoutes.Callbacks.Bot.My),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }

        public static InlineKeyboardMarkup MyBotsKeyboard(BotTask entity, IReadOnlyList<BotListItem>? bots)
        {
            var rows = new List<InlineKeyboardButton[]>();

            if (bots?.Count > 0)
            {
                rows.AddRange(bots.Select(b => new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: (b.IsEnabled ? "✅ " : "❌ ") + b.Title,
                        callbackData: BotRoutes.Callbacks.Bot.Edit(b.Id))
                }).ToList());
            }

            var addText = entity.AnswerLang == UiLang.En ? "➕ Add Bot" : "➕ Добавить бота";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(addText, BotRoutes.Callbacks.Bot.Add) });

            return new InlineKeyboardMarkup(rows);
        }

        public static InlineKeyboardMarkup? EditKeyboard(BotTask entity)
        {
            if (entity?.BotMirror is null) return null;

            var startStopText = entity.BotMirror.IsEnabled
                ? (entity.AnswerLang == UiLang.En ? "⏸ Stop" : "⏸ Остановить")
                : (entity.AnswerLang == UiLang.En ? "▶️ Start" : "▶️ Запустить");

            var startStop = entity.BotMirror.IsEnabled
                ? InlineKeyboardButton.WithCallbackData(startStopText, BotRoutes.Callbacks.Bot.Stop(entity.BotMirror.Id.ToString()))
                : InlineKeyboardButton.WithCallbackData(startStopText, BotRoutes.Callbacks.Bot.Start(entity.BotMirror.Id.ToString()));

            var deleteText = entity.AnswerLang == UiLang.En ? "🗑 Delete" : "🗑 Удалить";
            var backText = entity.AnswerLang == UiLang.En ? "◀️ Back" : "◀️ Назад";

            return new InlineKeyboardMarkup(new[]
            {
                new[] { startStop },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(deleteText, BotRoutes.Callbacks.Bot.Delete(entity.BotMirror.Id.ToString())),
                    InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Bot.My),
                }
            });
        }

        public static InlineKeyboardMarkup? DeleteConfirmKeyboard(BotTask entity)
        {
            if (entity?.BotMirror is null) return null;

            var yesText = entity.AnswerLang == UiLang.En ? "✅ Yes" : "✅ Да";
            var noText = entity.AnswerLang == UiLang.En ? "❌ No" : "❌ Нет";
            var backText = entity.AnswerLang == UiLang.En ? "◀️ Back" : "◀️ Назад";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(yesText, BotRoutes.Callbacks.Bot.DeleteYes(entity.BotMirror.Id.ToString())),
                    InlineKeyboardButton.WithCallbackData(noText, BotRoutes.Callbacks.Bot.DeleteNo(entity.BotMirror.Id.ToString())),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Bot.Edit(entity.BotMirror.Id.ToString())),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }
    }
}

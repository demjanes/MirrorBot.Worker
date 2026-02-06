using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Flow.Routes;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.UI
{
    public static class BotUi
    {
        public static UiText T(UiLang lang) => new(lang);
        public readonly struct UiText
        {
            private readonly UiLang _lang;
            public UiText(UiLang lang) => _lang = lang;

            public string ChangeLanguage => _lang == UiLang.En ? "Change language" : "Сменить язык";
            public string ChooseLanguage => _lang == UiLang.En ? "Choose language:" : "Выберите язык:";
            public string AskBotToken => _lang == UiLang.En ? "Send bot token:" : "Пришлите токен бота:";
            public string MyBots => _lang == UiLang.En ? "My bots" : "Мои боты";
            // …и так далее
        }



        public static class Text
        {
            public static string Start(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Hello! It's start message:");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Привет! Это стартовое сообщение");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string HideKbrd(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"HideKbrd done");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Клавиатура скрыта. Если потребуется повторно введите: {BotRoutes.Commands.Start}");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string Unknown(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Unknown command");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Неизвестная команда");
                            break;
                        }
                }
                return sb.ToString();
            }

            public static string Menu(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"MENU message:");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Меню");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string Help(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"HELP message:");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Помощь");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string Ref(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"REF message:");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Рефералка");
                            break;
                        }
                }
                return sb.ToString();
            }

            public static string LangChoose(TaskEntity entity)
            {
                var lang = entity.answerLang == UiLang.Ru ? "Русский" : "English";

                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Language switsher");
                            sb.AppendLine($"Current language: {lang}");
                            sb.AppendLine($"Choose your language if need");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Меню изменения языка");
                            sb.AppendLine($"Текущий язык: {lang}");
                            sb.AppendLine($"Выбери свой язык");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string LangSet(TaskEntity entity)
            {
                var lang = entity.answerLang == UiLang.Ru ? "Русский" : "English";

                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Language switsher");
                            sb.AppendLine($"Current language: {lang}");
                            sb.AppendLine($"Choose your language if need");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Меню изменения языка");
                            sb.AppendLine($"Язык изменен на {lang}");
                            sb.AppendLine($"Выбери свой язык");
                            break;
                        }
                }
                return sb.ToString();
            }

            public static string BotsMy(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Yours Bots");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Ваши боты");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string BotAdd(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Adding Bots");
                            sb.AppendLine($"Send Me Token of Bot");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Добавление бота");
                            sb.AppendLine($"Пришлите токен бота следующим сообщением.");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string BotAddResult(TaskEntity entity)
            {
                var bot = entity.mirrorBotEntity;
                if (bot is null) return "Данных нет";

                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Mirror @{bot.BotUsername} added. It will be start automatically.");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Зеркало @{bot.BotUsername} добавлено. Оно запустится автоматически.");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string BotEdit(TaskEntity entity)
            {
                var bot = entity.mirrorBotEntity;
                if (bot is null) return "Данных нет";

                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Bot @{bot.BotUsername}");
                            sb.AppendLine($"Status: {(bot.IsEnabled ? "ON" : "OFF")}.");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Бот @{bot.BotUsername}");
                            sb.AppendLine($"Состояние: {(bot.IsEnabled ? "включён" : "выключен")}.");
                            break;
                        }
                }
                return sb.ToString();
            }        
            public static string BotEditNotFound(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Bot is not found.");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Бот не найден.");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string BotEditNoAccess(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"No access to bot.");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Нет доступа к боту.");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string BotDeleteConfirm(TaskEntity entity)
            {
                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Delete bot??");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Удалить бота? Это действие нельзя отменить.");
                            break;
                        }
                }
                return sb.ToString();
            }
            public static string BotDeleteYesResult(TaskEntity entity)
            {
                var bot = entity.mirrorBotEntity;
                if (bot is null) return "Данных нет";

                var sb = new StringBuilder();
                switch (entity.answerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine($"Bot @{bot.BotUsername} delete.");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine($"Бот @{bot.BotUsername} удален.");
                            break;
                        }
                }
                return sb.ToString();
            }




            public const string AskBotToken = "Пришлите токен бота следующим сообщением.";
            public const string TokenAlreadyAdded = "Этот токен уже добавлен.";
            public static string MirrorAdded(string username) => $"Зеркало @{username} добавлено. Оно запустится автоматически.";

            public const string CallbackUnknown = "Неизвестная кнопка";
        }

        public static class Keyboards
        {
            public static ReplyKeyboardMarkup StartR(TaskEntity entity)
            {
                var kb = new[]
                    {
                        new KeyboardButton[] { new(BotRoutes.Commands.MenuTxt_Ru), new(BotRoutes.Commands.HelpTxt_Ru) },
                        new KeyboardButton[] { new(BotRoutes.Commands.HideKbrdTxt_Ru) },
                    };

                return new ReplyKeyboardMarkup(kb)
                {
                    ResizeKeyboard = true,   // подогнать размер
                    OneTimeKeyboard = false, // не скрывать автоматически после нажатия
                    InputFieldPlaceholder = "Выбери действие",
                    Selective = false
                };
            }

            public static InlineKeyboardMarkup Menu(TaskEntity entity)
            {
                var kb = new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Помощь", BotRoutes.Callbacks.Menu.Help),
                        InlineKeyboardButton.WithCallbackData("Рефералка", BotRoutes.Callbacks.Menu.Ref)
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Язык", BotRoutes.Callbacks.Lang.Choose),
                        InlineKeyboardButton.WithCallbackData("Мои боты", BotRoutes.Callbacks.Bot.My),
                    }

                };

                return new InlineKeyboardMarkup(kb);
            }
            public static InlineKeyboardMarkup Help(TaskEntity entity)
            {
                var kb = new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Меню", BotRoutes.Callbacks.Menu.MenuMain),
                        InlineKeyboardButton.WithCallbackData("Рефералка", BotRoutes.Callbacks.Menu.Ref)
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Язык", BotRoutes.Callbacks.Lang.Choose),
                        InlineKeyboardButton.WithCallbackData("Мои боты", BotRoutes.Callbacks.Bot.My),
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }
            public static InlineKeyboardMarkup Ref(TaskEntity entity)
            {
                var kb = new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Меню", BotRoutes.Callbacks.Menu.MenuMain),
                        InlineKeyboardButton.WithCallbackData("Помощь", BotRoutes.Callbacks.Menu.Help),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Язык", BotRoutes.Callbacks.Lang.Choose),
                        InlineKeyboardButton.WithCallbackData("Мои боты", BotRoutes.Callbacks.Bot.My),
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup LangChoose(TaskEntity entity)
            {
                var kb = new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(entity.answerLang == UiLang.Ru ? "Русский ✅" : "Русский", BotRoutes.Callbacks.Lang.Set(UiLang.Ru)),
                        InlineKeyboardButton.WithCallbackData(entity.answerLang == UiLang.En ? "English ✅" : "English", BotRoutes.Callbacks.Lang.Set(UiLang.En))
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Меню", BotRoutes.Callbacks.Menu.MenuMain)
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup BotAddResult(TaskEntity entity)
            {
                var kb = new[]
                {
                     
                     new []
                     {
                         InlineKeyboardButton.WithCallbackData("Мои боты", BotRoutes.Callbacks.Bot.My),
                     }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup BotsMy(TaskEntity entity, IReadOnlyList<BotListItem>? bots)
            {
                var rows = new List<InlineKeyboardButton[]>();

                if (bots?.Count > 0)
                {
                    rows.AddRange(bots.Select(b =>
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(text: $"{(b.IsEnabled ? "🟢" : "🔴")} {b.Title}", callbackData: BotRoutes.Callbacks.Bot.Edit(b.Id))
                        })
                    .ToList());
                }

                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить", BotRoutes.Callbacks.Bot.Add)
                });

                return new InlineKeyboardMarkup(rows);
            }
            public static InlineKeyboardMarkup BotEdit(TaskEntity entity)
            {
                if (entity is null) return null;
                if (entity.mirrorBotEntity is null) return null;

                var startStop = entity.mirrorBotEntity.IsEnabled
                    ? InlineKeyboardButton.WithCallbackData("⏸ Stop", BotRoutes.Callbacks.Bot.Stop(entity.mirrorBotEntity.Id.ToString()))
                    : InlineKeyboardButton.WithCallbackData("▶️ Start", BotRoutes.Callbacks.Bot.Start(entity.mirrorBotEntity.Id.ToString()));

                return new InlineKeyboardMarkup(new[]
                {
                    new [] { startStop },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("🗑 Удалить", BotRoutes.Callbacks.Bot.Delete(entity.mirrorBotEntity.Id.ToString())),
                        InlineKeyboardButton.WithCallbackData("↩️ Мои боты", BotRoutes.Callbacks.Bot.My),
                    }
                });
            }
            public static InlineKeyboardMarkup BotDeleteConfirm(TaskEntity entity)
            {
                if (entity is null) return null;
                if (entity.mirrorBotEntity is null) return null;

                var kb = new[]
               {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Да, удалить", BotRoutes.Callbacks.Bot.DeleteYes(entity.mirrorBotEntity.Id.ToString())),
                        InlineKeyboardButton.WithCallbackData("❌ Нет", BotRoutes.Callbacks.Bot.DeleteNo(entity.mirrorBotEntity.Id.ToString())),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("↩️ Назад", BotRoutes.Callbacks.Bot.Edit(entity.mirrorBotEntity.Id.ToString())),
                    }
               };

                return new InlineKeyboardMarkup(kb);
            }












            public sealed record BotListItem(string Id, string Title, bool IsEnabled);        
          
        }
    }
}

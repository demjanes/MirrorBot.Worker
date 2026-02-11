using MirrorBot.Worker.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Flow.UI
{
    public readonly struct UiText
    {
        private readonly UiLang _lang;

        public UiText(UiLang lang) => _lang = lang;

        public string ChangeLanguage => _lang == UiLang.En ? "Change language" : "Сменить язык";
        public string ChooseLanguage => _lang == UiLang.En ? "Choose language:" : "Выберите язык:";
        public string AskBotToken => _lang == UiLang.En ? "Send bot token:" : "Пришлите токен бота:";
        public string MyBots => _lang == UiLang.En ? "My bots" : "Мои боты";
    }

    public static class BotUi
    {
        public static UiText T(UiLang lang) => new(lang);
    }
}

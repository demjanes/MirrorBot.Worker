using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Flow.Handlers;
using MongoDB.Bson;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MirrorBot.Worker.Flow.Routes
{
    public static class BotRoutes
    {
        public static class Commands
        {
            public const string Start = "/start";
            public const string HideKbrdTxt_Ru = "❌ Скрыть клавиатуру";
            public const string HideKbrdTxt_En = "❌ Hide keyboard";

           

            public const string Menu = "/menu";
            public const string MenuTxt_Ru = "📋 Меню";
            public const string MenuTxt_En = "📋 Menu";

            public const string Help = "/help";
            public const string HelpTxt_Ru = "ℹ️ Помощь";
            public const string HelpTxt_En = "ℹ️ Help";


            public const string Ref = "/ref";



            public const string AddBot = "/addbot";
        }

        public static class Callbacks
        {
            public static class Menu
            {
                public const string _section = "menu";

                public static readonly string MenuMainAction = "menu";
                public static readonly string MenuMain = CbCodec.Pack(_section, MenuMainAction);
                
                public static readonly string HelpAction = "help";
                public static readonly string Help = CbCodec.Pack(_section, HelpAction);

                public static readonly string RefAction = "ref";
                public static readonly string Ref = CbCodec.Pack(_section, RefAction);
            }

            public static class Lang
            {
                public const string _section = "lang";

                public static readonly string ChooseAction = "choose";
                public static readonly string Choose = CbCodec.Pack(_section, ChooseAction);

                public static readonly string SetAction = "set";
                public static string Set(UiLang lang) => CbCodec.Pack(_section, SetAction, lang.ToString());
            }

            public static class Bot
            {
                public const string _section = "bot";

                public static readonly string MyAction = "my";
                public static readonly string My = CbCodec.Pack(_section, MyAction);

                public static readonly string AddAction = "add";
                public static readonly string Add = CbCodec.Pack(_section, AddAction);

                public static readonly string EditAction = "edit";
                public static string Edit(string objectId) => CbCodec.Pack(_section, EditAction, objectId);

                public static readonly string StopAction = "stop";
                public static string Stop(string objectId) => CbCodec.Pack(_section, StopAction, objectId);

                public static readonly string StartAction = "start";
                public static string Start(string objectId) => CbCodec.Pack(_section, StartAction, objectId);

                public static readonly string DeleteAction = "delete";
                public static string Delete(string objectId) => CbCodec.Pack(_section, DeleteAction, objectId);
                
                public static readonly string DeleteYesAction = "delete_yes";
                public static string DeleteYes(string objectId) => CbCodec.Pack(_section, DeleteYesAction, objectId);
                
                public static readonly string DeleteNoAction = "delete_no";
                public static string DeleteNo(string objectId) => CbCodec.Pack(_section, DeleteNoAction, objectId);




            }
        }
    }
}

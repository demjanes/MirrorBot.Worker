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
            public const string RefTxt_Ru = "💰 Реферальная программа";
            public const string RefTxt_En = "💰 Referral Program";

            public const string AddBot = "/addbot";

            public const string Subscription = "/sub";
            public const string SubscriptionTxt_Ru = "💎 Подписка";
            public const string SubscriptionTxt_En = "💎 Subscription";

            public const string Payments = "/payments";
            public const string PaymentsTxt_Ru = "💳 Мои платежи";
            public const string PaymentsTxt_En = "💳 My Payments";
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

            // ============ НОВОЕ: Секция реферальной программы ============
            public static class Referral
            {
                public const string _section = "referral";

                // Главное меню
                public static readonly string MainAction = "main";
                public static readonly string Main = CbCodec.Pack(_section, MainAction);

                // Статистика
                public static readonly string StatsAction = "stats";
                public static readonly string Stats = CbCodec.Pack(_section, StatsAction);

                // Реферальные ссылки
                public static readonly string LinksAction = "links";
                public static readonly string Links = CbCodec.Pack(_section, LinksAction);

                // История транзакций
                public static readonly string TransactionsAction = "txns";
                public static readonly string Transactions = CbCodec.Pack(_section, TransactionsAction);

                // Настройки уведомлений
                public static readonly string SettingsAction = "settings";
                public static readonly string Settings = CbCodec.Pack(_section, SettingsAction);

                // Переключатели уведомлений
                public static readonly string ToggleNewReferralAction = "toggle_new";
                public static readonly string ToggleNewReferral = CbCodec.Pack(_section, ToggleNewReferralAction);

                public static readonly string ToggleEarningsAction = "toggle_earn";
                public static readonly string ToggleEarnings = CbCodec.Pack(_section, ToggleEarningsAction);

                public static readonly string TogglePayoutAction = "toggle_payout";
                public static readonly string TogglePayout = CbCodec.Pack(_section, TogglePayoutAction);

                // Запрос на вывод средств
                public static readonly string PayoutAction = "payout";
                public static readonly string Payout = CbCodec.Pack(_section, PayoutAction);
            }

            // ============ Секция подписок ============
            public static class Subscription
            {
                public const string _section = "sub";

                // Главное меню подписок
                public static readonly string MainAction = "main";
                public static readonly string Main = CbCodec.Pack(_section, MainAction);

                // Просмотр текущей подписки
                public static readonly string ViewAction = "view";
                public static readonly string View = CbCodec.Pack(_section, ViewAction);

                // Выбор Premium тарифа
                public static readonly string ChoosePlanAction = "choose";
                public static readonly string ChoosePlan = CbCodec.Pack(_section, ChoosePlanAction);

                // Покупка конкретного плана
                public static readonly string BuyAction = "buy";
                public static string Buy(string planId) => CbCodec.Pack(_section, BuyAction, planId);

                // Отмена подписки
                public static readonly string CancelAction = "cancel";
                public static readonly string Cancel = CbCodec.Pack(_section, CancelAction);

                // Подтверждение отмены
                public static readonly string CancelYesAction = "cancel_yes";
                public static readonly string CancelYes = CbCodec.Pack(_section, CancelYesAction);

                public static readonly string CancelNoAction = "cancel_no";
                public static readonly string CancelNo = CbCodec.Pack(_section, CancelNoAction);

                public static readonly string PaymentsAction = "payments";
                public static readonly string Payments = CbCodec.Pack(_section, PaymentsAction);
            }
        }
    }
}

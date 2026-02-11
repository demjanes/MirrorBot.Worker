using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Models.Subscription;
using MirrorBot.Worker.Flow.Routes;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.UI
{
    public record SubscriptionPlanItem(
    string Id,
    string Name,
    decimal PriceRub,
    int DurationDays,
    bool IsCurrentPlan);


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
            public static string Start(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string HideKbrd(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string Unknown(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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

            public static string Menu(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string Help(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string Ref(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine("💰 <b>Referral Program</b>");
                            sb.AppendLine();
                            sb.AppendLine("Invite users to your bots and earn:");
                            sb.AppendLine("• Get notifications about new referrals");
                            sb.AppendLine("• Track statistics and earnings");
                            sb.AppendLine("• Withdraw funds");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine("💰 <b>Реферальная программа</b>");
                            sb.AppendLine();
                            sb.AppendLine("Приглашай пользователей в свои боты и зарабатывай:");
                            sb.AppendLine("• Получай уведомления о новых рефералах");
                            sb.AppendLine("• Отслеживай статистику и заработок");
                            sb.AppendLine("• Выводи средства");
                            break;
                        }
                }
                return sb.ToString();
            }

            public static string ReferralStats(Data.Models.Core.BotTask entity, ReferralStats? stats)
            {
                if (stats == null)
                {
                    return entity.AnswerLang == UiLang.En
                        ? "📊 <b>Statistics</b>\n\nNo data yet."
                        : "📊 <b>Статистика</b>\n\nДанных пока нет.";
                }

                // Вычисляем выведенную сумму
                var totalWithdrawn = stats.TotalReferralRevenue - stats.Balance;

                var sb = new StringBuilder();
                switch (entity.AnswerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine("📊 <b>Referral Statistics</b>");
                            sb.AppendLine();
                            sb.AppendLine($"👥 Total referrals: <b>{stats.TotalReferrals}</b>");
                            sb.AppendLine($"💳 Paying referrals: <b>{stats.PaidReferrals}</b>");
                            sb.AppendLine();
                            sb.AppendLine($"💰 Total earned: <b>{stats.TotalReferralRevenue:F2} {stats.Currency}</b>");
                            sb.AppendLine($"💵 Balance: <b>{stats.Balance:F2} {stats.Currency}</b>");
                            sb.AppendLine($"📤 Withdrawn: <b>{totalWithdrawn:F2} {stats.Currency}</b>");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine("📊 <b>Статистика рефералов</b>");
                            sb.AppendLine();
                            sb.AppendLine($"👥 Всего рефералов: <b>{stats.TotalReferrals}</b>");
                            sb.AppendLine($"💳 Платящих рефералов: <b>{stats.PaidReferrals}</b>");
                            sb.AppendLine();
                            sb.AppendLine($"💰 Всего заработано: <b>{stats.TotalReferralRevenue:F2} {stats.Currency}</b>");
                            sb.AppendLine($"💵 Баланс: <b>{stats.Balance:F2} {stats.Currency}</b>");
                            sb.AppendLine($"📤 Выведено: <b>{totalWithdrawn:F2} {stats.Currency}</b>");
                            break;
                        }
                }
                return sb.ToString();
            }

            public static string ReferralLinks(Data.Models.Core.BotTask entity, List<string> links)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine("🔗 <b>Referral Links</b>");
                            sb.AppendLine();
                            if (links.Count == 0)
                            {
                                sb.AppendLine("You don't have any bots yet.");
                                sb.AppendLine("Add bots to get referral links.");
                            }
                            else
                            {
                                sb.AppendLine("Share these links to invite users:");
                                sb.AppendLine();
                                foreach (var link in links)
                                {
                                    sb.AppendLine($"• <code>{link}</code>");
                                }
                            }
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine("🔗 <b>Реферальные ссылки</b>");
                            sb.AppendLine();
                            if (links.Count == 0)
                            {
                                sb.AppendLine("У вас пока нет ботов.");
                                sb.AppendLine("Добавьте ботов, чтобы получить реферальные ссылки.");
                            }
                            else
                            {
                                sb.AppendLine("Делитесь этими ссылками для приглашения пользователей:");
                                sb.AppendLine();
                                foreach (var link in links)
                                {
                                    sb.AppendLine($"• <code>{link}</code>");
                                }
                            }
                            break;
                        }
                }
                return sb.ToString();
            }

            public static string ReferralTransactions(Data.Models.Core.BotTask entity, List<ReferralTransaction> txns)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine("📜 <b>Transaction History</b>");
                            sb.AppendLine();
                            if (txns.Count == 0)
                            {
                                sb.AppendLine("No transactions yet.");
                            }
                            else
                            {
                                foreach (var txn in txns.Take(10))
                                {
                                    var kind = txn.Kind == ReferralTransactionKind.Accrual ? "➕" : "➖";
                                    var date = txn.CreatedAtUtc.ToString("dd.MM.yyyy HH:mm");
                                    sb.AppendLine($"{kind} <b>{txn.Amount:F2} {txn.Currency}</b>");
                                    sb.AppendLine($"   {txn.Description}");
                                    sb.AppendLine($"   {date}");
                                    sb.AppendLine();
                                }
                                if (txns.Count > 10)
                                {
                                    sb.AppendLine($"<i>Showing last 10 of {txns.Count} transactions</i>");
                                }
                            }
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine("📜 <b>История транзакций</b>");
                            sb.AppendLine();
                            if (txns.Count == 0)
                            {
                                sb.AppendLine("Транзакций пока нет.");
                            }
                            else
                            {
                                foreach (var txn in txns.Take(10))
                                {
                                    var kind = txn.Kind == ReferralTransactionKind.Accrual ? "➕" : "➖";
                                    var date = txn.CreatedAtUtc.ToString("dd.MM.yyyy HH:mm");
                                    sb.AppendLine($"{kind} <b>{txn.Amount:F2} {txn.Currency}</b>");
                                    sb.AppendLine($"   {txn.Description}");
                                    sb.AppendLine($"   {date}");
                                    sb.AppendLine();
                                }
                                if (txns.Count > 10)
                                {
                                    sb.AppendLine($"<i>Показаны последние 10 из {txns.Count} транзакций</i>");
                                }
                            }
                            break;
                        }
                }
                return sb.ToString();
            }

            public static string ReferralSettings(Data.Models.Core.BotTask entity, MirrorBotOwnerSettings settings)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
                {
                    case (UiLang.En):
                        {
                            sb.AppendLine("⚙️ <b>Notification Settings</b>");
                            sb.AppendLine();
                            sb.AppendLine($"New referrals: {(settings.NotifyOnNewReferral ? "✅ ON" : "❌ OFF")}");
                            sb.AppendLine($"Balance updates: {(settings.NotifyOnReferralEarnings ? "✅ ON" : "❌ OFF")}");
                            sb.AppendLine($"Withdrawals: {(settings.NotifyOnPayout ? "✅ ON" : "❌ OFF")}");
                            break;
                        }
                    default:
                    case (UiLang.Ru):
                        {
                            sb.AppendLine("⚙️ <b>Настройки уведомлений</b>");
                            sb.AppendLine();
                            sb.AppendLine($"Новые рефералы: {(settings.NotifyOnNewReferral ? "✅ ВКЛ" : "❌ ВЫКЛ")}");
                            sb.AppendLine($"Пополнения баланса: {(settings.NotifyOnReferralEarnings ? "✅ ВКЛ" : "❌ ВЫКЛ")}");
                            sb.AppendLine($"Выводы средств: {(settings.NotifyOnPayout ? "✅ ВКЛ" : "❌ ВЫКЛ")}");
                            break;
                        }
                }
                return sb.ToString();
            }

            public static string LangChoose(Data.Models.Core.BotTask entity)
            {
                var lang = entity.AnswerLang == UiLang.Ru ? "Русский" : "English";

                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string LangSet(Data.Models.Core.BotTask entity)
            {
                var lang = entity.AnswerLang == UiLang.Ru ? "Русский" : "English";

                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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

            public static string BotsMy(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string BotAdd(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string BotAddResult(Data.Models.Core.BotTask entity)
            {
                var bot = entity.BotMirror;
                if (bot is null) return "Данных нет";

                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string BotEdit(Data.Models.Core.BotTask entity)
            {
                var bot = entity.BotMirror;
                if (bot is null) return "Данных нет";

                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string BotEditNotFound(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string BotEditNoAccess(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string BotDeleteConfirm(Data.Models.Core.BotTask entity)
            {
                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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
            public static string BotDeleteYesResult(Data.Models.Core.BotTask entity)
            {
                var bot = entity.BotMirror;
                if (bot is null) return "Данных нет";

                var sb = new StringBuilder();
                switch (entity.AnswerLang)
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





            /// <summary>
            /// Информация о текущей подписке
            /// </summary>
            public static string SubscriptionInfo(
                BotTask entity,
                SubscriptionInfo info)
            {
                var sb = new StringBuilder();

                switch (entity.AnswerLang)
                {
                    case UiLang.En:
                        sb.AppendLine("💎 <b>Your Subscription</b>\n");

                        sb.AppendLine($"📌 Plan: <b>{info.TypeName}</b>");

                        if (info.IsPremium)
                        {
                            sb.AppendLine($"⏰ Expires: <b>{info.ExpiresAt:yyyy-MM-dd HH:mm UTC}</b>");
                            sb.AppendLine($"📅 Days remaining: <b>{info.DaysRemaining}</b>\n");

                            sb.AppendLine("✨ <b>Premium Features:</b>");
                            sb.AppendLine("✅ Unlimited text messages");
                            sb.AppendLine("✅ Voice messages support");
                            sb.AppendLine("✅ Grammar corrections");
                            sb.AppendLine("✅ Vocabulary tracking");
                            sb.AppendLine("✅ Priority support");
                        }
                        else
                        {
                            sb.AppendLine($"📊 Daily limit: <b>{info.DailyTextLimit} messages</b>");
                            sb.AppendLine($"📈 Used today: <b>{info.TextMessagesUsedToday}/{info.DailyTextLimit}</b>\n");

                            sb.AppendLine("⚠️ <b>Free Plan Limitations:</b>");
                            sb.AppendLine("❌ Voice messages disabled");
                            sb.AppendLine("❌ Limited text messages");
                            sb.AppendLine("❌ Basic features only\n");

                            sb.AppendLine("💡 Upgrade to Premium for unlimited access!");
                        }
                        break;

                    default:
                    case UiLang.Ru:
                        sb.AppendLine("💎 <b>Ваша подписка</b>\n");

                        sb.AppendLine($"📌 Тариф: <b>{info.TypeName}</b>");

                        if (info.IsPremium)
                        {
                            sb.AppendLine($"⏰ Истекает: <b>{info.ExpiresAt:yyyy-MM-dd HH:mm UTC}</b>");
                            sb.AppendLine($"📅 Осталось дней: <b>{info.DaysRemaining}</b>\n");

                            sb.AppendLine("✨ <b>Premium возможности:</b>");
                            sb.AppendLine("✅ Безлимитные текстовые сообщения");
                            sb.AppendLine("✅ Поддержка голосовых сообщений");
                            sb.AppendLine("✅ Проверка грамматики");
                            sb.AppendLine("✅ Отслеживание словарного запаса");
                            sb.AppendLine("✅ Приоритетная поддержка");
                        }
                        else
                        {
                            sb.AppendLine($"📊 Дневной лимит: <b>{info.DailyTextLimit} сообщений</b>");
                            sb.AppendLine($"📈 Использовано сегодня: <b>{info.TextMessagesUsedToday}/{info.DailyTextLimit}</b>\n");

                            sb.AppendLine("⚠️ <b>Ограничения Free тарифа:</b>");
                            sb.AppendLine("❌ Голосовые сообщения недоступны");
                            sb.AppendLine("❌ Ограниченное количество сообщений");
                            sb.AppendLine("❌ Только базовые функции\n");

                            sb.AppendLine("💡 Перейдите на Premium для безлимитного доступа!");
                        }
                        break;
                }

                return sb.ToString();
            }

            /// <summary>
            /// Список доступных Premium планов
            /// </summary>
            public static string SubscriptionPlans(
                BotTask entity,
                List<SubscriptionPlanItem> plans)
            {
                var sb = new StringBuilder();

                switch (entity.AnswerLang)
                {
                    case UiLang.En:
                        sb.AppendLine("💎 <b>Premium Plans</b>\n");
                        sb.AppendLine("Choose the plan that suits you:\n");

                        foreach (var plan in plans)
                        {
                            var discount = plan.DurationDays switch
                            {
                                90 => " (Save 10%)",
                                180 => " (Save 20%)",
                                365 => " (Save 30%)",
                                _ => ""
                            };

                            sb.AppendLine($"• <b>{plan.Name}</b>");
                            sb.AppendLine($"  💰 {plan.PriceRub:N0} ₽{discount}");
                            sb.AppendLine($"  📅 {plan.DurationDays} days\n");
                        }

                        sb.AppendLine("✨ All Premium plans include:");
                        sb.AppendLine("✅ Unlimited messages");
                        sb.AppendLine("✅ Voice messages");
                        sb.AppendLine("✅ Grammar corrections");
                        sb.AppendLine("✅ Vocabulary tracking");
                        break;

                    default:
                    case UiLang.Ru:
                        sb.AppendLine("💎 <b>Premium тарифы</b>\n");
                        sb.AppendLine("Выберите подходящий тариф:\n");

                        foreach (var plan in plans)
                        {
                            var discount = plan.DurationDays switch
                            {
                                90 => " (Скидка 10%)",
                                180 => " (Скидка 20%)",
                                365 => " (Скидка 30%)",
                                _ => ""
                            };

                            sb.AppendLine($"• <b>{plan.Name}</b>");
                            sb.AppendLine($"  💰 {plan.PriceRub:N0} ₽{discount}");
                            sb.AppendLine($"  📅 {plan.DurationDays} дней\n");
                        }

                        sb.AppendLine("✨ Все Premium тарифы включают:");
                        sb.AppendLine("✅ Безлимитные сообщения");
                        sb.AppendLine("✅ Голосовые сообщения");
                        sb.AppendLine("✅ Проверку грамматики");
                        sb.AppendLine("✅ Отслеживание слов");
                        break;
                }

                return sb.ToString();
            }

            /// <summary>
            /// Подтверждение отмены подписки
            /// </summary>
            public static string SubscriptionCancelConfirm(BotTask entity)
            {
                return entity.AnswerLang switch
                {
                    UiLang.En =>
                        "⚠️ <b>Cancel Subscription?</b>\n\n" +
                        "Your Premium subscription will remain active until the end of the current period.\n\n" +
                        "Are you sure you want to cancel?",
                    _ =>
                        "⚠️ <b>Отменить подписку?</b>\n\n" +
                        "Ваша Premium подписка останется активной до конца текущего периода.\n\n" +
                        "Вы уверены, что хотите отменить?"
                };
            }

            /// <summary>
            /// Результат отмены подписки
            /// </summary>
            public static string SubscriptionCanceled(BotTask entity)
            {
                return entity.AnswerLang switch
                {
                    UiLang.En =>
                        "✅ <b>Subscription Canceled</b>\n\n" +
                        "Your Premium subscription has been canceled.\n" +
                        "You can still use Premium features until the end of the current period.",
                    _ =>
                        "✅ <b>Подписка отменена</b>\n\n" +
                        "Ваша Premium подписка отменена.\n" +
                        "Вы можете использовать Premium функции до конца текущего периода."
                };
            }




            /// <summary>
            /// Текст с ссылкой на оплату.
            /// </summary>
            public static string PaymentLink(BotTask entity)
            {
                return entity.AnswerLang switch
                {
                    UiLang.En =>
                        "💳 <b>Payment Link</b>\n\n" +
                        "Click the button below to complete the payment.\n\n" +
                        "After successful payment, your Premium subscription will be activated automatically.",
                    _ =>
                        "💳 <b>Ссылка для оплаты</b>\n\n" +
                        "Нажмите на кнопку ниже для завершения оплаты.\n\n" +
                        "После успешной оплаты ваша Premium подписка будет активирована автоматически."
                };
            }














            public const string AskBotToken = "Пришлите токен бота следующим сообщением.";
            public const string TokenAlreadyAdded = "Этот токен уже добавлен.";
            public static string MirrorAdded(string username) => $"Зеркало @{username} добавлено. Оно запустится автоматически.";

            public const string CallbackUnknown = "Неизвестная кнопка";
        }

        public static class Keyboards
        {
            public static ReplyKeyboardMarkup StartR(Data.Models.Core.BotTask entity)
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

            public static InlineKeyboardMarkup Menu(Data.Models.Core.BotTask entity)
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
            public static InlineKeyboardMarkup Help(Data.Models.Core.BotTask entity)
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
            public static InlineKeyboardMarkup Ref(Data.Models.Core.BotTask entity)
            {
                var kb = new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("📊 Статистика", BotRoutes.Callbacks.Referral.Stats),
                        InlineKeyboardButton.WithCallbackData("🔗 Ссылки", BotRoutes.Callbacks.Referral.Links),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("📜 Транзакции", BotRoutes.Callbacks.Referral.Transactions),
                        InlineKeyboardButton.WithCallbackData("⚙️ Настройки", BotRoutes.Callbacks.Referral.Settings),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("↩️ Меню", BotRoutes.Callbacks.Menu.MenuMain),
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup ReferralStats(Data.Models.Core.BotTask entity)
            {
                var kb = new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("↩️ Назад", BotRoutes.Callbacks.Referral.Main),
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup ReferralLinks(Data.Models.Core.BotTask entity)
            {
                var kb = new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("↩️ Назад", BotRoutes.Callbacks.Referral.Main),
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup ReferralTransactions(Data.Models.Core.BotTask entity)
            {
                var kb = new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("↩️ Назад", BotRoutes.Callbacks.Referral.Main),
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup ReferralSettings(Data.Models.Core.BotTask entity, MirrorBotOwnerSettings settings)
            {
                var newRefText = settings.NotifyOnNewReferral
                    ? "🔕 Новые рефералы"
                    : "🔔 Новые рефералы";

                var earningsText = settings.NotifyOnReferralEarnings
                    ? "🔕 Пополнения"
                    : "🔔 Пополнения";

                var payoutText = settings.NotifyOnPayout
                    ? "🔕 Выводы"
                    : "🔔 Выводы";

                var kb = new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(newRefText, BotRoutes.Callbacks.Referral.ToggleNewReferral),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(earningsText, BotRoutes.Callbacks.Referral.ToggleEarnings),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(payoutText, BotRoutes.Callbacks.Referral.TogglePayout),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("↩️ Назад", BotRoutes.Callbacks.Referral.Main),
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup LangChoose(Data.Models.Core.BotTask entity)
            {
                var kb = new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(entity.AnswerLang == UiLang.Ru ? "Русский ✅" : "Русский", BotRoutes.Callbacks.Lang.Set(UiLang.Ru)),
                        InlineKeyboardButton.WithCallbackData(entity.AnswerLang == UiLang.En ? "English ✅" : "English", BotRoutes.Callbacks.Lang.Set(UiLang.En))
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Меню", BotRoutes.Callbacks.Menu.MenuMain)
                    }
                };

                return new InlineKeyboardMarkup(kb);
            }

            public static InlineKeyboardMarkup BotAddResult(Data.Models.Core.BotTask entity)
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

            public static InlineKeyboardMarkup BotsMy(Data.Models.Core.BotTask entity, IReadOnlyList<BotListItem>? bots)
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
            public static InlineKeyboardMarkup BotEdit(Data.Models.Core.BotTask entity)
            {
                if (entity is null) return null;
                if (entity.BotMirror is null) return null;

                var startStop = entity.BotMirror.IsEnabled
                    ? InlineKeyboardButton.WithCallbackData("⏸ Stop", BotRoutes.Callbacks.Bot.Stop(entity.BotMirror.Id.ToString()))
                    : InlineKeyboardButton.WithCallbackData("▶️ Start", BotRoutes.Callbacks.Bot.Start(entity.BotMirror.Id.ToString()));

                return new InlineKeyboardMarkup(new[]
                {
                    new [] { startStop },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("🗑 Удалить", BotRoutes.Callbacks.Bot.Delete(entity.BotMirror.Id.ToString())),
                        InlineKeyboardButton.WithCallbackData("↩️ Мои боты", BotRoutes.Callbacks.Bot.My),
                    }
                });
            }
            public static InlineKeyboardMarkup BotDeleteConfirm(Data.Models.Core.BotTask entity)
            {
                if (entity is null) return null;
                if (entity.BotMirror is null) return null;

                var kb = new[]
               {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Да, удалить", BotRoutes.Callbacks.Bot.DeleteYes(entity.BotMirror.Id.ToString())),
                        InlineKeyboardButton.WithCallbackData("❌ Нет", BotRoutes.Callbacks.Bot.DeleteNo(entity.BotMirror.Id.ToString())),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("↩️ Назад", BotRoutes.Callbacks.Bot.Edit(entity.BotMirror.Id.ToString())),
                    }
               };

                return new InlineKeyboardMarkup(kb);
            }




            /// <summary>
            /// Главное меню подписки
            /// </summary>
            public static InlineKeyboardMarkup SubscriptionInfo(BotTask entity, bool isPremium)
            {
                var buttons = new List<List<InlineKeyboardButton>>();

                if (!isPremium)
                {
                    // Для Free - кнопка перехода на Premium
                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    entity.AnswerLang == UiLang.En ? "💎 Upgrade to Premium" : "💎 Перейти на Premium",
                    BotRoutes.Callbacks.Subscription.ChoosePlan)
            });
                }
                else
                {
                    // Для Premium - кнопка отмены
                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    entity.AnswerLang == UiLang.En ? "❌ Cancel Subscription" : "❌ Отменить подписку",
                    BotRoutes.Callbacks.Subscription.Cancel)
            });
                }

                // Кнопка "Назад"
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(
                entity.AnswerLang == UiLang.En ? "⬅️ Back" : "⬅️ Назад",
                BotRoutes.Callbacks.Menu.MenuMain)
        });

                return new InlineKeyboardMarkup(buttons);
            }

            /// <summary>
            /// Выбор Premium плана
            /// </summary>
            public static InlineKeyboardMarkup SubscriptionPlans(
                BotTask entity,
                List<SubscriptionPlanItem> plans)
            {
                var buttons = new List<List<InlineKeyboardButton>>();

                // Кнопка для каждого плана
                foreach (var plan in plans)
                {
                    buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{plan.Name} — {plan.PriceRub:N0} ₽",
                    BotRoutes.Callbacks.Subscription.Buy(plan.Id))
            });
                }

                // Кнопка "Назад"
                buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(
                entity.AnswerLang == UiLang.En ? "⬅️ Back" : "⬅️ Назад",
                BotRoutes.Callbacks.Subscription.Main)
        });

                return new InlineKeyboardMarkup(buttons);
            }

            /// <summary>
            /// Подтверждение отмены подписки
            /// </summary>
            public static InlineKeyboardMarkup SubscriptionCancelConfirm(BotTask entity)
            {
                var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    entity.AnswerLang == UiLang.En ? "✅ Yes, Cancel" : "✅ Да, отменить",
                    BotRoutes.Callbacks.Subscription.CancelYes),
                InlineKeyboardButton.WithCallbackData(
                    entity.AnswerLang == UiLang.En ? "❌ No, Keep" : "❌ Нет, оставить",
                    BotRoutes.Callbacks.Subscription.CancelNo)
            }
        };

                return new InlineKeyboardMarkup(buttons);
            }


            /// <summary>
            /// Клавиатура с ссылкой на оплату.
            /// </summary>
            public static InlineKeyboardMarkup PaymentLink(BotTask entity, string paymentUrl)
            {
                var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithUrl(
                entity.AnswerLang == UiLang.En ? "💳 Pay" : "💳 Оплатить",
                paymentUrl)
        },
        new()
        {
            InlineKeyboardButton.WithCallbackData(
                entity.AnswerLang == UiLang.En ? "⬅️ Back" : "⬅️ Назад",
                BotRoutes.Callbacks.Subscription.ChoosePlan)
        }
    };

                return new InlineKeyboardMarkup(buttons);
            }




            public sealed record BotListItem(string Id, string Title, bool IsEnabled);        
          
        }
    }
}

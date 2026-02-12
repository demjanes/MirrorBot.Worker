using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI.Models;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.UI
{
    public static class UiReferral
    {
        public static string Ref(BotTask entity)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("<b>Referral Program</b>");
                    sb.AppendLine();
                    sb.AppendLine("• Invite users to your bots and earn");
                    sb.AppendLine("• Get notifications about new referrals");
                    sb.AppendLine("• Track statistics and earnings");
                    sb.AppendLine("• Withdraw funds");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("<b>Реферальная программа</b>");
                    sb.AppendLine();
                    sb.AppendLine("• Приглашайте пользователей в своих ботов и зарабатывайте");
                    sb.AppendLine("• Получайте уведомления о новых рефералах");
                    sb.AppendLine("• Отслеживайте статистику и доходы");
                    sb.AppendLine("• Выводите средства");
                    break;
            }
            return sb.ToString();
        }

        public static string Stats(BotTask entity, ReferralStats? stats)
        {
            if (stats is null)
                return entity.AnswerLang == UiLang.En
                    ? "<b>Statistics</b>\n\nNo data yet."
                    : "<b>Статистика</b>\n\nДанных пока нет.";

            var totalWithdrawn = stats.TotalReferralRevenue - stats.Balance;
            var sb = new StringBuilder();

            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("<b>Referral Statistics</b>");
                    sb.AppendLine();
                    sb.AppendLine($"👥 Total referrals: <b>{stats.TotalReferrals}</b>");
                    sb.AppendLine($"💳 Paying referrals: <b>{stats.PaidReferrals}</b>");
                    sb.AppendLine();
                    sb.AppendLine($"💰 Total earned: <b>{stats.TotalReferralRevenue:F2} {stats.Currency}</b>");
                    sb.AppendLine($"💵 Balance: <b>{stats.Balance:F2} {stats.Currency}</b>");
                    sb.AppendLine($"📤 Withdrawn: <b>{totalWithdrawn:F2} {stats.Currency}</b>");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("<b>Реферальная статистика</b>");
                    sb.AppendLine();
                    sb.AppendLine($"👥 Всего рефералов: <b>{stats.TotalReferrals}</b>");
                    sb.AppendLine($"💳 Платящих рефералов: <b>{stats.PaidReferrals}</b>");
                    sb.AppendLine();
                    sb.AppendLine($"💰 Всего заработано: <b>{stats.TotalReferralRevenue:F2} {stats.Currency}</b>");
                    sb.AppendLine($"💵 Баланс: <b>{stats.Balance:F2} {stats.Currency}</b>");
                    sb.AppendLine($"📤 Выведено: <b>{totalWithdrawn:F2} {stats.Currency}</b>");
                    break;
            }

            return sb.ToString();
        }

        public static string Links(BotTask entity, List<string> links)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("<b>Referral Links</b>");
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
                            sb.AppendLine(link);
                    }
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("<b>Реферальные ссылки</b>");
                    sb.AppendLine();
                    if (links.Count == 0)
                    {
                        sb.AppendLine("У вас пока нет ботов.");
                        sb.AppendLine("Добавьте ботов, чтобы получить реферальные ссылки.");
                    }
                    else
                    {
                        sb.AppendLine("Поделитесь этими ссылками для приглашения пользователей:");
                        sb.AppendLine();
                        foreach (var link in links)
                            sb.AppendLine(link);
                    }
                    break;
            }
            return sb.ToString();
        }

        public static string Transactions(BotTask entity, List<ReferralTransaction> txns)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("<b>Transaction History</b>");
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
                            sb.AppendLine(txn.Description);
                            sb.AppendLine($"📅 {date}");
                            sb.AppendLine();
                        }
                        if (txns.Count > 10)
                            sb.AppendLine($"<i>Showing last 10 of {txns.Count} transactions</i>");
                    }
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("<b>История транзакций</b>");
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
                            sb.AppendLine(txn.Description);
                            sb.AppendLine($"📅 {date}");
                            sb.AppendLine();
                        }
                        if (txns.Count > 10)
                            sb.AppendLine($"<i>Показаны последние 10 из {txns.Count}</i>");
                    }
                    break;
            }
            return sb.ToString();
        }

        public static string Settings(BotTask entity, MirrorBotOwnerSettings settings)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("<b>Notification Settings</b>");
                    sb.AppendLine();
                    sb.AppendLine($"👤 New referrals: {(settings.NotifyOnNewReferral ? "✅ ON" : "❌ OFF")}");
                    sb.AppendLine($"💵 Balance updates: {(settings.NotifyOnReferralEarnings ? "✅ ON" : "❌ OFF")}");
                    sb.AppendLine($"📤 Withdrawals: {(settings.NotifyOnPayout ? "✅ ON" : "❌ OFF")}");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("<b>Настройки уведомлений</b>");
                    sb.AppendLine();
                    sb.AppendLine($"👤 Новые рефералы: {(settings.NotifyOnNewReferral ? "✅ ВКЛ" : "❌ ВЫКЛ")}");
                    sb.AppendLine($"💵 Изменения баланса: {(settings.NotifyOnReferralEarnings ? "✅ ВКЛ" : "❌ ВЫКЛ")}");
                    sb.AppendLine($"📤 Выплаты: {(settings.NotifyOnPayout ? "✅ ВКЛ" : "❌ ВЫКЛ")}");
                    break;
            }
            return sb.ToString();
        }

        // Keyboards
        public static InlineKeyboardMarkup RefKeyboard(BotTask entity)
        {
            var statsText = entity.AnswerLang == UiLang.En ? "📊 Statistics" : "📊 Статистика";
            var linksText = entity.AnswerLang == UiLang.En ? "🔗 Links" : "🔗 Ссылки";
            var transText = entity.AnswerLang == UiLang.En ? "📜 History" : "📜 История";
            var settingsText = entity.AnswerLang == UiLang.En ? "⚙️ Settings" : "⚙️ Настройки";
            var menuText = entity.AnswerLang == UiLang.En ? "📋 Menu" : "📋 Меню";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(statsText, BotRoutes.Callbacks.Referral.Stats),
                    InlineKeyboardButton.WithCallbackData(linksText, BotRoutes.Callbacks.Referral.Links),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(transText, BotRoutes.Callbacks.Referral.Transactions),
                    InlineKeyboardButton.WithCallbackData(settingsText, BotRoutes.Callbacks.Referral.Settings),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(menuText, BotRoutes.Callbacks.Menu.MenuMain),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }

        public static InlineKeyboardMarkup StatsKeyboard(BotTask entity)
        {
            var backText = entity.AnswerLang == UiLang.En ? "◀️ Back" : "◀️ Назад";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Referral.Main),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }

        public static InlineKeyboardMarkup LinksKeyboard(BotTask entity)
        {
            var backText = entity.AnswerLang == UiLang.En ? "◀️ Back" : "◀️ Назад";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Referral.Main),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }

        public static InlineKeyboardMarkup TransactionsKeyboard(BotTask entity)
        {
            var backText = entity.AnswerLang == UiLang.En ? "◀️ Back" : "◀️ Назад";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Referral.Main),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }

        public static InlineKeyboardMarkup SettingsKeyboard(BotTask entity, MirrorBotOwnerSettings settings)
        {
            var newRefText = (settings.NotifyOnNewReferral ? "✅" : "❌") + (entity.AnswerLang == UiLang.En ? " New referrals" : " Новые рефералы");
            var earningsText = (settings.NotifyOnReferralEarnings ? "✅" : "❌") + (entity.AnswerLang == UiLang.En ? " Balance updates" : " Изменения баланса");
            var payoutText = (settings.NotifyOnPayout ? "✅" : "❌") + (entity.AnswerLang == UiLang.En ? " Withdrawals" : " Выплаты");
            var backText = entity.AnswerLang == UiLang.En ? "◀️ Back" : "◀️ Назад";

            var kb = new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(newRefText, BotRoutes.Callbacks.Referral.ToggleNewReferral),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(earningsText, BotRoutes.Callbacks.Referral.ToggleEarnings),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(payoutText, BotRoutes.Callbacks.Referral.TogglePayout),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Referral.Main),
                }
            };

            return new InlineKeyboardMarkup(kb);
        }
    }
}

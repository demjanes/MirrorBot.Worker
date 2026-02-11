using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Models.Payments;
using MirrorBot.Worker.Data.Models.Subscription;
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
    public static class SubscriptionUi
    {
        public static string Info(BotTask entity, SubscriptionInfo info)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("<b>💎 Your Subscription</b>");
                    sb.AppendLine();
                    sb.AppendLine($"📋 Plan: <b>{info.TypeName}</b>");
                    if (info.IsPremium)
                    {
                        sb.AppendLine($"⏰ Expires: <b>{info.ExpiresAt:yyyy-MM-dd HH:mm}</b> UTC");
                        sb.AppendLine($"📅 Days remaining: <b>{info.DaysRemaining}</b>");
                        sb.AppendLine();
                        sb.AppendLine("<b>Premium Features:</b>");
                        sb.AppendLine("✅ Unlimited text messages");
                        sb.AppendLine("✅ Voice messages support");
                        sb.AppendLine("✅ Grammar corrections");
                        sb.AppendLine("✅ Vocabulary tracking");
                        sb.AppendLine("✅ Priority support");
                    }
                    else
                    {
                        sb.AppendLine($"📊 Daily limit: <b>{info.DailyTextLimit}</b> messages");
                        sb.AppendLine($"📈 Used today: <b>{info.TextMessagesUsedToday}/{info.DailyTextLimit}</b>");
                        sb.AppendLine();
                        sb.AppendLine("<b>Free Plan Limitations:</b>");
                        sb.AppendLine("❌ Voice messages disabled");
                        sb.AppendLine("❌ Limited text messages");
                        sb.AppendLine("❌ Basic features only");
                        sb.AppendLine();
                        sb.AppendLine("⭐ Upgrade to Premium for unlimited access!");
                    }
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("<b>💎 Ваша подписка</b>");
                    sb.AppendLine();
                    sb.AppendLine($"📋 План: <b>{info.TypeName}</b>");
                    if (info.IsPremium)
                    {
                        sb.AppendLine($"⏰ Истекает: <b>{info.ExpiresAt:yyyy-MM-dd HH:mm}</b> UTC");
                        sb.AppendLine($"📅 Осталось дней: <b>{info.DaysRemaining}</b>");
                        sb.AppendLine();
                        sb.AppendLine("<b>Premium возможности:</b>");
                        sb.AppendLine("✅ Неограниченные текстовые сообщения");
                        sb.AppendLine("✅ Поддержка голосовых сообщений");
                        sb.AppendLine("✅ Грамматические исправления");
                        sb.AppendLine("✅ Отслеживание словарного запаса");
                        sb.AppendLine("✅ Приоритетная поддержка");
                    }
                    else
                    {
                        sb.AppendLine($"📊 Дневной лимит: <b>{info.DailyTextLimit}</b> сообщений");
                        sb.AppendLine($"📈 Использовано сегодня: <b>{info.TextMessagesUsedToday}/{info.DailyTextLimit}</b>");
                        sb.AppendLine();
                        sb.AppendLine("<b>Ограничения Free плана:</b>");
                        sb.AppendLine("❌ Голосовые сообщения отключены");
                        sb.AppendLine("❌ Ограниченные текстовые сообщения");
                        sb.AppendLine("❌ Только базовые функции");
                        sb.AppendLine();
                        sb.AppendLine("⭐ Обновитесь до Premium для неограниченного доступа!");
                    }
                    break;
            }
            return sb.ToString();
        }

        public static string Plans(BotTask entity, List<SubscriptionPlanItem> plans)
        {
            var sb = new StringBuilder();
            switch (entity.AnswerLang)
            {
                case UiLang.En:
                    sb.AppendLine("<b>⭐ Premium Plans</b>");
                    sb.AppendLine("Choose the plan that suits you:");
                    sb.AppendLine();
                    foreach (var plan in plans)
                    {
                        var discount = plan.DurationDays switch
                        {
                            90 => " (Save 10%)",
                            180 => " (Save 20%)",
                            365 => " (Save 30%)",
                            _ => ""
                        };
                        sb.AppendLine($"💎 <b>{plan.Name}</b>");
                        sb.AppendLine($"💰 {plan.PriceRub:N0} ₽{discount}");
                        sb.AppendLine($"📅 {plan.DurationDays} days");
                        sb.AppendLine();
                    }
                    sb.AppendLine("<b>All Premium plans include:</b>");
                    sb.AppendLine("✅ Unlimited messages");
                    sb.AppendLine("✅ Voice messages");
                    sb.AppendLine("✅ Grammar corrections");
                    sb.AppendLine("✅ Vocabulary tracking");
                    break;
                default:
                case UiLang.Ru:
                    sb.AppendLine("<b>⭐ Premium планы</b>");
                    sb.AppendLine("Выберите подходящий план:");
                    sb.AppendLine();
                    foreach (var plan in plans)
                    {
                        var discount = plan.DurationDays switch
                        {
                            90 => " (Экономия 10%)",
                            180 => " (Экономия 20%)",
                            365 => " (Экономия 30%)",
                            _ => ""
                        };
                        sb.AppendLine($"💎 <b>{plan.Name}</b>");
                        sb.AppendLine($"💰 {plan.PriceRub:N0} ₽{discount}");
                        sb.AppendLine($"📅 {plan.DurationDays} дней");
                        sb.AppendLine();
                    }
                    sb.AppendLine("<b>Все Premium планы включают:</b>");
                    sb.AppendLine("✅ Неограниченные сообщения");
                    sb.AppendLine("✅ Голосовые сообщения");
                    sb.AppendLine("✅ Грамматические исправления");
                    sb.AppendLine("✅ Отслеживание словаря");
                    break;
            }
            return sb.ToString();
        }

        public static string CancelConfirm(BotTask entity)
        {
            return entity.AnswerLang switch
            {
                UiLang.En => "<b>❌ Cancel Subscription?</b>\n\n" +
                    "Your Premium subscription will remain active until the end of the current period.\n\n" +
                    "Are you sure you want to cancel?",
                _ => "<b>❌ Отменить подписку?</b>\n\n" +
                    "Ваша Premium подписка останется активной до конца текущего периода.\n\n" +
                    "Вы уверены, что хотите отменить?"
            };
        }

        public static string Canceled(BotTask entity)
        {
            return entity.AnswerLang switch
            {
                UiLang.En => "<b>✅ Subscription Canceled</b>\n\n" +
                    "Your Premium subscription has been canceled. " +
                    "You can still use Premium features until the end of the current period.",
                _ => "<b>✅ Подписка отменена</b>\n\n" +
                    "Ваша Premium подписка была отменена. " +
                    "Вы все еще можете использовать Premium функции до конца текущего периода."
            };
        }

        public static string PaymentLink(BotTask t)
        {
            return t.AnswerLang switch
            {
                UiLang.En => "<b>💳 Payment Link</b>\n\n" +
                    "Click the button below to proceed with payment. " +
                    "The link is valid for 15 minutes.",
                _ => "<b>💳 Ссылка для оплаты</b>\n\n" +
                    "Нажмите на кнопку ниже, чтобы продолжить оплату. " +
                    "Ссылка действительна 15 минут."
            };
        }

        public static string PaymentSuccess(BotTask t, string planName, DateTime expiresAt)
        {
            var expiresStr = expiresAt.ToString("dd.MM.yyyy HH:mm");
            return t.AnswerLang switch
            {
                UiLang.En => $"<b>✅ Payment Successful!</b>\n\n" +
                    $"Your {planName} subscription is now active.\n" +
                    $"Valid until: <code>{expiresStr}</code>",
                _ => $"<b>✅ Оплата прошла успешно!</b>\n\n" +
                    $"Ваша подписка {planName} теперь активна.\n" +
                    $"Действительна до: <code>{expiresStr}</code>"
            };
        }

        public static string PaymentError(BotTask t, string? errorMessage = null)
        {
            var error = string.IsNullOrEmpty(errorMessage) ? "" : $"\n{errorMessage}";
            return t.AnswerLang switch
            {
                UiLang.En => $"<b>❌ Payment Error</b>{error}\n\nPlease try again or contact support.",
                _ => $"<b>❌ Ошибка оплаты</b>{error}\n\nПопробуйте снова или обратитесь в поддержку."
            };
        }

        public static string PaymentSuccessNotification(UiLang lang, string planName, decimal amount, DateTime expiresAt)
        {
            var expiresStr = expiresAt.ToString("dd.MM.yyyy HH:mm");
            return lang switch
            {
                UiLang.En => $"<b>✅ Payment Successful!</b>\n\n" +
                    $"Your {planName} subscription is now active.\n" +
                    $"Amount: {amount:F2} ₽\n" +
                    $"Valid until: <code>{expiresStr}</code>\n\n" +
                    $"Thank you for your purchase! 🎉",
                _ => $"<b>✅ Оплата прошла успешно!</b>\n\n" +
                    $"Ваша подписка {planName} теперь активна.\n" +
                    $"Сумма: {amount:F2} ₽\n" +
                    $"Действительна до: <code>{expiresStr}</code>\n\n" +
                    $"Спасибо за покупку! 🎉"
            };
        }

        public static string PaymentCanceledNotification(UiLang lang, decimal amount)
        {
            return lang switch
            {
                UiLang.En => $"<b>❌ Payment Canceled</b>\n\n" +
                    $"Your payment of {amount:F2} ₽ was canceled.\n" +
                    $"If you want to subscribe, please try again.",
                _ => $"<b>❌ Оплата отменена</b>\n\n" +
                    $"Ваш платеж на сумму {amount:F2} ₽ был отменен.\n" +
                    $"Если хотите оформить подписку, попробуйте снова."
            };
        }

        public static string PaymentFailedNotification(UiLang lang, decimal amount, string? errorMessage = null)
        {
            var error = string.IsNullOrEmpty(errorMessage) ? "" : $"\n{errorMessage}";
            return lang switch
            {
                UiLang.En => $"<b>⛔ Payment Error</b>\n\n" +
                    $"There was an error processing your payment of {amount:F2} ₽.{error}\n\n" +
                    $"Please try again or contact support.",
                _ => $"<b>⛔ Ошибка оплаты</b>\n\n" +
                    $"Произошла ошибка при обработке платежа на сумму {amount:F2} ₽.{error}\n\n" +
                    $"Попробуйте снова или обратитесь в поддержку."
            };
        }

        public static string UserPayments(BotTask t, List<Payment> payments)
        {
            if (payments.Count == 0)
            {
                return t.AnswerLang switch
                {
                    UiLang.En => "<b>💳 Payment History</b>\n\nYou have no payments yet.",
                    _ => "<b>💳 История платежей</b>\n\nУ вас пока нет платежей."
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine(t.AnswerLang switch
            {
                UiLang.En => "<b>💳 Payment History</b>",
                _ => "<b>💳 История платежей</b>"
            });
            sb.AppendLine();

            foreach (var payment in payments.Take(10))
            {
                var statusEmoji = payment.Status switch
                {
                    PaymentStatus.Succeeded => "✅",
                    PaymentStatus.Pending => "⏳",
                    PaymentStatus.Canceled => "❌",
                    PaymentStatus.Failed => "⛔",
                    _ => "❓"
                };

                var statusText = payment.Status switch
                {
                    PaymentStatus.Succeeded => t.AnswerLang == UiLang.En ? "Paid" : "Оплачено",
                    PaymentStatus.Pending => t.AnswerLang == UiLang.En ? "Pending" : "Ожидание",
                    PaymentStatus.Canceled => t.AnswerLang == UiLang.En ? "Canceled" : "Отменено",
                    PaymentStatus.Failed => t.AnswerLang == UiLang.En ? "Failed" : "Ошибка",
                    _ => "Unknown"
                };

                var dateStr = payment.CreatedAtUtc.ToString("dd.MM.yyyy HH:mm");
                var planName = payment.Metadata?.GetValueOrDefault("planname") ?? "Unknown";

                sb.AppendLine($"{statusEmoji} <b>{planName}</b>");
                sb.AppendLine($"💰 {payment.Amount:F2} ₽ • {statusText}");
                sb.AppendLine($"📅 {dateStr}");
                sb.AppendLine();
            }

            if (payments.Count > 10)
            {
                sb.AppendLine(t.AnswerLang switch
                {
                    UiLang.En => $"<i>Showing last 10 of {payments.Count} payments</i>",
                    _ => $"<i>Показаны последние 10 из {payments.Count} платежей</i>"
                });
            }

            return sb.ToString();
        }

        // Keyboards
        public static InlineKeyboardMarkup InfoKeyboard(BotTask t, bool isPremium)
        {
            var upgradeText = t.AnswerLang == UiLang.En ? "⭐ Upgrade to Premium" : "⭐ Перейти на Premium";
            var cancelText = t.AnswerLang == UiLang.En ? "❌ Cancel Subscription" : "❌ Отменить подписку";
            var paymentsText = t.AnswerLang == UiLang.En ? "💳 Payment History" : "💳 История платежей";
            var backText = t.AnswerLang == UiLang.En ? "◀️ Back to Menu" : "◀️ Назад в меню";

            var buttons = new List<InlineKeyboardButton[]>();

            if (!isPremium)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(upgradeText, BotRoutes.Callbacks.Subscription.ChoosePlan)
                });
            }
            else
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(cancelText, BotRoutes.Callbacks.Subscription.Cancel)
                });
            }

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(paymentsText, BotRoutes.Callbacks.Subscription.Payments)
            });

            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Menu.MenuMain)
            });

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup PlansKeyboard(BotTask entity, List<SubscriptionPlanItem> plans)
        {
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var plan in plans)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"💎 {plan.Name} • {plan.PriceRub:N0} ₽",
                        BotRoutes.Callbacks.Subscription.Buy(plan.Id))
                });
            }

            var backText = entity.AnswerLang == UiLang.En ? "◀️ Back" : "◀️ Назад";
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Subscription.Main)
            });

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup CancelConfirmKeyboard(BotTask entity)
        {
            var yesText = entity.AnswerLang == UiLang.En ? "✅ Yes, Cancel" : "✅ Да, отменить";
            var noText = entity.AnswerLang == UiLang.En ? "❌ No, Keep" : "❌ Нет, оставить";

            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(yesText, BotRoutes.Callbacks.Subscription.CancelYes),
                    InlineKeyboardButton.WithCallbackData(noText, BotRoutes.Callbacks.Subscription.CancelNo)
                }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        public static InlineKeyboardMarkup PaymentLinkKeyboard(BotTask t, string paymentUrl)
        {
            var payText = t.AnswerLang == UiLang.En ? "💳 Pay Now" : "💳 Оплатить сейчас";
            var cancelText = t.AnswerLang == UiLang.En ? "❌ Cancel" : "❌ Отмена";

            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl(payText, paymentUrl),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(cancelText, BotRoutes.Callbacks.Subscription.Main),
                }
            });
        }

        public static InlineKeyboardMarkup UserPaymentsKeyboard(BotTask t)
        {
            var backText = t.AnswerLang == UiLang.En ? "◀️ Back" : "◀️ Назад";

            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(backText, BotRoutes.Callbacks.Subscription.Main),
                }
            });
        }
    }
}

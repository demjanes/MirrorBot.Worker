using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Models.Core;
using MirrorBot.Worker.Data.Repositories.Interfaces;
using MirrorBot.Worker.Flow.Handlers.Models;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI;
using MirrorBot.Worker.Flow.UI.Models;
using MirrorBot.Worker.Services.AdminNotifierService;
using MirrorBot.Worker.Services.Payments;
using MirrorBot.Worker.Services.Referral;
using MirrorBot.Worker.Services.Subscr;
using MongoDB.Bson;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.Handlers
{
    public sealed class BotCallbackHandler
    {
        private readonly IUsersRepository _users;
        private readonly IMirrorBotsRepository _mirrorBots;
        private readonly IAdminNotifier _notifier;
        private readonly IReferralService _referralService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMirrorBotOwnerSettingsRepository _ownerSettingsRepo;
        private readonly IPaymentService _paymentService;

        public BotCallbackHandler(
            IUsersRepository users,
            IMirrorBotsRepository mirrorBots,
            IAdminNotifier notifier,
            IReferralService referralService,
            ISubscriptionService subscriptionService,
            IMirrorBotOwnerSettingsRepository ownerSettingsRepo,
            IPaymentService paymentService)
        {
            _users = users;
            _mirrorBots = mirrorBots;
            _notifier = notifier;
            _referralService = referralService;
            _subscriptionService = subscriptionService;
            _ownerSettingsRepo = ownerSettingsRepo;
            _paymentService = paymentService;
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, CallbackQuery cq, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(cq.Data)) return;

            var cb = CbCodec.TryUnpack(cq.Data);
            if (cb is null) return;

            var chatId = cq.Message?.Chat.Id ?? cq.From.Id;

            var t = new Data.Models.Core.BotTask
            {
                BotContext = ctx,
                TgClient = client,
                TgChatId = chatId,
                TgCallbackQuery = cq,
            };

            await UpsertSeenAsync(t, ct);

            // Чтобы "часики" не крутились
            await client.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

            if (string.Equals(cb.Section, BotRoutes.Callbacks.Menu._section, StringComparison.OrdinalIgnoreCase))
            {
                await HandleMenuAsync(t, cb, ct);
                return;
            }

            if (string.Equals(cb.Section, BotRoutes.Callbacks.Lang._section, StringComparison.OrdinalIgnoreCase))
            {
                await HandleLangAsync(t, cb, ct);
                return;
            }

            if (string.Equals(cb.Section, BotRoutes.Callbacks.Bot._section, StringComparison.OrdinalIgnoreCase))
            {
                await HandleBotsAsync(t, cb, ct);
                return;
            }

            if (string.Equals(cb.Section, BotRoutes.Callbacks.Referral._section, StringComparison.OrdinalIgnoreCase))
            {
                await HandleReferralAsync(t, cb, ct);
                return;
            }

            if (string.Equals(cb.Section, BotRoutes.Callbacks.Subscription._section, StringComparison.OrdinalIgnoreCase))
            {
                await HandleSubscriptionAsync(t, cb, ct);
                return;
            }
        }

        private async Task HandleMenuAsync(BotTask t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Menu.MenuMainAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = UiCommon.Menu(t);
                    t.AnswerKeyboard = UiCommon.MenuKeyboard(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Menu.HelpAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = UiCommon.Help(t);
                    t.AnswerKeyboard = UiCommon.HelpKeyboard(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Menu.RefAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = UiReferral.Ref(t);
                    t.AnswerKeyboard = UiReferral.RefKeyboard(t);
                    await SendOrEditAsync(t, ct);
                    return;

                default:
                    return;
            }
        }

        private async Task HandleLangAsync(BotTask t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Lang.ChooseAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = UiLanguage.Choose(t);
                    t.AnswerKeyboard = UiLanguage.ChooseKeyboard(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Lang.SetAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var args = cb.Args ?? Array.Empty<string>();
                        var newLang = UiLangExt.ParseOrDefault(args.ElementAtOrDefault(0), UiLang.Ru);

                        t.User = await _users.SetPreferredLangAsync(
                            t.TgCallbackQuery!.From.Id,
                            newLang,
                            DateTime.UtcNow,
                            ct);

                        t.AnswerText = UiLanguage.Set(t);
                        t.AnswerKeyboard = UiLanguage.ChooseKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                default:
                    return;
            }
        }

        private async Task HandleBotsAsync(BotTask t, CbData cb, CancellationToken ct)
        {
            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Bot.AddAction, StringComparison.OrdinalIgnoreCase):
                    t.AnswerText = UiBotManagement.Add(t);
                    await SendOrEditAsync(t, ct);
                    return;

                case string s when s.Equals(BotRoutes.Callbacks.Bot.MyAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var ownerId = t.TgCallbackQuery!.From.Id;
                        var bots = await _mirrorBots.GetByOwnerTgIdAsync(ownerId, ct);

                        var items = bots.Select(b => new BotListItem(
                            Id: b.Id.ToString(),
                            Title: "@" + (b.BotUsername ?? "unknown"),
                            IsEnabled: b.IsEnabled)).ToList();

                        t.AnswerText = UiBotManagement.MyBots(t);
                        t.AnswerKeyboard = UiBotManagement.MyBotsKeyboard(t, items);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.EditAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.AnswerText = UiBotManagement.Edit(t);
                        t.AnswerKeyboard = UiBotManagement.EditKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.StopAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        var nowUtc = DateTime.UtcNow;
                        t.BotMirror = await _mirrorBots.SetEnabledAsync(t.BotMirror!.Id, false, nowUtc, ct);

                        t.AnswerText = UiBotManagement.Edit(t);
                        t.AnswerKeyboard = UiBotManagement.EditKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.StartAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        var nowUtc = DateTime.UtcNow;
                        t.BotMirror = await _mirrorBots.SetEnabledAsync(t.BotMirror!.Id, true, nowUtc, ct);

                        t.AnswerText = UiBotManagement.Edit(t);
                        t.AnswerKeyboard = UiBotManagement.EditKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.AnswerText = UiBotManagement.DeleteConfirm(t);
                        t.AnswerKeyboard = UiBotManagement.DeleteConfirmKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteYesAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        await _mirrorBots.DeleteAsync(t.BotMirror!.Id, ct);

                        t.AnswerText = UiBotManagement.DeleteYesResult(t);
                        t.AnswerKeyboard = UiBotManagement.MyBotsKeyboard(t, null);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteNoAction, StringComparison.OrdinalIgnoreCase):
                    {
                        if (!await TryLoadOwnedBotAsync(t, cb, 0, ct)) return;

                        t.AnswerText = UiBotManagement.Edit(t);
                        t.AnswerKeyboard = UiBotManagement.EditKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                default:
                    return;
            }
        }

        private async Task HandleReferralAsync(BotTask t, CbData cb, CancellationToken ct)
        {
            var ownerId = t.TgCallbackQuery!.From.Id;

            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Referral.MainAction, StringComparison.OrdinalIgnoreCase):
                    {
                        t.AnswerText = UiReferral.Ref(t);
                        t.AnswerKeyboard = UiReferral.RefKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Referral.StatsAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var stats = await _referralService.GetOwnerStatsAsync(ownerId, ct);
                        t.AnswerText = UiReferral.Stats(t, stats);
                        t.AnswerKeyboard = UiReferral.StatsKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Referral.LinksAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var bots = await _mirrorBots.GetByOwnerTgIdAsync(ownerId, ct);

                        var links = bots
                            .Where(b => !string.IsNullOrEmpty(b.BotUsername))
                            .Select(b => $"https://t.me/{b.BotUsername}?start={ownerId}")
                            .ToList();

                        t.AnswerText = UiReferral.Links(t, links);
                        t.AnswerKeyboard = UiReferral.LinksKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Referral.TransactionsAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var transactions = await _referralService.GetOwnerTransactionsAsync(ownerId, limit: 100, ct);
                        t.AnswerText = UiReferral.Transactions(t, transactions);
                        t.AnswerKeyboard = UiReferral.TransactionsKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Referral.SettingsAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var settings = await _ownerSettingsRepo.GetOrCreateAsync(ownerId, ct);
                        t.AnswerText = UiReferral.Settings(t, settings);
                        t.AnswerKeyboard = UiReferral.SettingsKeyboard(t, settings);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Referral.ToggleNewReferralAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var settings = await _ownerSettingsRepo.GetOrCreateAsync(ownerId, ct);

                        await _ownerSettingsRepo.UpdateNotificationSettingsAsync(
                            ownerId,
                            notifyOnNewReferral: !settings.NotifyOnNewReferral,
                            notifyOnReferralEarnings: null,
                            notifyOnPayout: null,
                            ct);

                        settings = await _ownerSettingsRepo.GetOrCreateAsync(ownerId, ct);
                        t.AnswerText = UiReferral.Settings(t, settings);
                        t.AnswerKeyboard = UiReferral.SettingsKeyboard(t, settings);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Referral.ToggleEarningsAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var settings = await _ownerSettingsRepo.GetOrCreateAsync(ownerId, ct);

                        await _ownerSettingsRepo.UpdateNotificationSettingsAsync(
                            ownerId,
                            notifyOnNewReferral: null,
                            notifyOnReferralEarnings: !settings.NotifyOnReferralEarnings,
                            notifyOnPayout: null,
                            ct);

                        settings = await _ownerSettingsRepo.GetOrCreateAsync(ownerId, ct);
                        t.AnswerText = UiReferral.Settings(t, settings);
                        t.AnswerKeyboard = UiReferral.SettingsKeyboard(t, settings);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Referral.TogglePayoutAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var settings = await _ownerSettingsRepo.GetOrCreateAsync(ownerId, ct);

                        await _ownerSettingsRepo.UpdateNotificationSettingsAsync(
                            ownerId,
                            notifyOnNewReferral: null,
                            notifyOnReferralEarnings: null,
                            notifyOnPayout: !settings.NotifyOnPayout,
                            ct);

                        settings = await _ownerSettingsRepo.GetOrCreateAsync(ownerId, ct);
                        t.AnswerText = UiReferral.Settings(t, settings);
                        t.AnswerKeyboard = UiReferral.SettingsKeyboard(t, settings);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                default:
                    return;
            }
        }

        private async Task HandleSubscriptionAsync(BotTask t, CbData cb, CancellationToken ct)
        {
            var userId = t.TgCallbackQuery!.From.Id;

            switch (cb.Action)
            {
                case string s when s.Equals(BotRoutes.Callbacks.Subscription.MainAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var subscriptionInfo = await _subscriptionService.GetSubscriptionInfoAsync(userId, ct);

                        t.AnswerText = UiSubscription.Info(t, subscriptionInfo);
                        t.AnswerKeyboard = UiSubscription.InfoKeyboard(t, subscriptionInfo.IsPremium);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Subscription.ChoosePlanAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var plans = await _subscriptionService.GetAvailablePremiumPlansAsync(ct);

                        var planItems = plans.Select(p => new SubscriptionPlanItem(
                            Id: p.Id.ToString(),
                            Name: p.Name,
                            PriceRub: p.PriceRub,
                            DurationDays: p.DurationDays,
                            IsCurrentPlan: false)).ToList();

                        t.AnswerText = UiSubscription.Plans(t, planItems);
                        t.AnswerKeyboard = UiSubscription.PlansKeyboard(t, planItems);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                // ✅ ДОЛЖЕН БЫТЬ ТОЛЬКО ЭТОТ case для BuyAction
                case string s when s.Equals(BotRoutes.Callbacks.Subscription.BuyAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var args = cb.Args ?? Array.Empty<string>();
                        if (!TryGetObjectId(args, 0, out var planId))
                            return;

                        var botUsername = t.BotContext?.BotUsername ?? "unknown_bot";

                        var (success, paymentUrl, errorMessage) = await _paymentService.CreatePaymentAsync(
                            userId,
                            planId,
                            botUsername,
                            provider: null,
                            cancellationToken: ct);

                        if (!success)
                        {
                            t.AnswerText = t.AnswerLang == UiLang.En
                                ? $"❌ Error: {errorMessage}"
                                : $"❌ Ошибка: {errorMessage}";
                            await SendOrEditAsync(t, ct);
                            return;
                        }

                        t.AnswerText = UiSubscription.PaymentLink(t);
                        t.AnswerKeyboard = UiSubscription.PaymentLinkKeyboard(t, paymentUrl!);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Subscription.CancelAction, StringComparison.OrdinalIgnoreCase):
                    {
                        t.AnswerText = UiSubscription.CancelConfirm(t);
                        t.AnswerKeyboard = UiSubscription.CancelConfirmKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Subscription.CancelYesAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var (success, errorMessage) = await _subscriptionService.CancelSubscriptionAsync(userId, ct);

                        if (!success)
                        {
                            t.AnswerText = t.AnswerLang == UiLang.En
                                ? $"❌ Error: {errorMessage}"
                                : $"❌ Ошибка: {errorMessage}";
                            await SendOrEditAsync(t, ct);
                            return;
                        }

                        t.AnswerText = UiSubscription.Canceled(t);
                        t.AnswerKeyboard = UiSubscription.InfoKeyboard(t, isPremium: false);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case string s when s.Equals(BotRoutes.Callbacks.Subscription.CancelNoAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var subscriptionInfo = await _subscriptionService.GetSubscriptionInfoAsync(userId, ct);

                        t.AnswerText = UiSubscription.Info(t, subscriptionInfo);
                        t.AnswerKeyboard = UiSubscription.InfoKeyboard(t, subscriptionInfo.IsPremium);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                case var cmd when cmd.Equals(BotRoutes.Callbacks.Subscription.PaymentsAction, StringComparison.OrdinalIgnoreCase):
                    {
                        var payments = await _paymentService.GetUserPaymentsAsync(userId, ct);

                        t.AnswerText = UiSubscription.UserPayments(t, payments);
                        t.AnswerKeyboard = UiSubscription.UserPaymentsKeyboard(t);
                        await SendOrEditAsync(t, ct);
                        return;
                    }

                default:
                    return;
            }
        }

        private async Task<bool> TryLoadOwnedBotAsync(BotTask t, CbData cb, int index, CancellationToken ct)
        {
            var args = cb.Args ?? Array.Empty<string>();

            if (!TryGetObjectId(args, index, out var botId))
                return false;

            t.BotMirror = await _mirrorBots.GetByIdAsync(botId, ct);
            if (t.BotMirror is null)
            {
                t.AnswerText = UiBotManagement.EditNotFound(t);
                t.AnswerKeyboard = UiBotManagement.MyBotsKeyboard(t, null);
                await SendOrEditAsync(t, ct);
                return false;
            }

            // security check: только владелец
            if (t.BotMirror.OwnerTelegramUserId != t.TgCallbackQuery!.From.Id)
            {
                t.AnswerText = UiBotManagement.EditNoAccess(t);
                await SendOrEditAsync(t, ct);
                return false;
            }

            return true;
        }

        private static async Task SendOrEditAsync(BotTask entity, CancellationToken ct)
        {
            if (entity?.TgClient is null) return;
            if (entity.TgCallbackQuery is null) return;
            if (entity.AnswerText is null) return;

            var chatId = entity.TgChatId ?? entity.TgCallbackQuery.From.Id;

            if (entity.TgCallbackQuery.Message is not { } m)
            {
                await entity.TgClient.SendMessage(
                    chatId: chatId,
                    text: entity.AnswerText,
                    parseMode: ParseMode.Html,
                    replyMarkup: entity.AnswerKeyboard,
                    cancellationToken: ct);
                return;
            }

            try
            {
                await entity.TgClient.EditMessageText(
                    chatId: m.Chat.Id,
                    messageId: m.MessageId,
                    text: entity.AnswerText,
                    parseMode: ParseMode.Html,
                    replyMarkup: entity.AnswerKeyboard as InlineKeyboardMarkup,
                    cancellationToken: ct);
            }
            catch (ApiRequestException ex) when (
                ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("message not found", StringComparison.OrdinalIgnoreCase))
            {
                if (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                    return;

                await entity.TgClient.SendMessage(
                    chatId: m.Chat.Id,
                    text: entity.AnswerText,
                    parseMode: ParseMode.Html,
                    replyMarkup: entity.AnswerKeyboard,
                    cancellationToken: ct);
            }
        }

        private static bool TryGetObjectId(string[] args, int index, out ObjectId id)
        {
            id = ObjectId.Empty;
            if (args is null) return false;
            if (args.Length <= index) return false;
            return ObjectId.TryParse(args[index], out id);
        }

        private async Task UpsertSeenAsync(Data.Models.Core.BotTask entity, CancellationToken ct)
        {
            if (entity?.BotContext is null) return;
            if (entity.TgCallbackQuery?.From is not { } from) return;

            var nowUtc = DateTime.UtcNow;

            var lastBotKey = entity.BotContext.MirrorBotId == ObjectId.Empty
                ? "__main__"
                : entity.BotContext.MirrorBotId.ToString();

            long? refOwner = null;
            ObjectId? refBotId = null;

            if (entity.BotContext.OwnerTelegramUserId != 0 &&
                entity.BotContext.MirrorBotId != ObjectId.Empty &&
                from.Id != entity.BotContext.OwnerTelegramUserId)
            {
                refOwner = entity.BotContext.OwnerTelegramUserId;
                refBotId = entity.BotContext.MirrorBotId;
            }

            var seen = new UserSeenEvent(
                TgUserId: from.Id,
                TgUsername: from.Username,
                TgFirstName: from.FirstName,
                TgLastName: from.LastName,
                TgLangCode: from.LanguageCode,
                LastBotKey: lastBotKey,
                LastChatId: entity.TgChatId,
                SeenAtUtc: nowUtc,
                ReferrerOwnerTgUserId: refOwner,
                ReferrerMirrorBotId: refBotId
            );

            _notifier.TryEnqueue(
                AdminChannel.Info,
                $"#id{seen.TgUserId} @{seen.TgUsername}\n" +
                $"/{entity.TgCallbackQuery.Data}\n" +
                $"@{entity.BotContext.BotUsername}");

            var (user, isNewUser) = await _users.UpsertSeenAsync(seen, ct);

            entity.User = user;
        }
    }
}

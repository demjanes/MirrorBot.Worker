using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Enums;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI;
using MirrorBot.Worker.Services.AdminNotifierService;
using MongoDB.Bson;
using SharpCompress.Common;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static MirrorBot.Worker.Flow.UI.BotUi.Keyboards;

namespace MirrorBot.Worker.Flow.Handlers
{
    public sealed class BotCallbackHandler
    {
        private readonly UsersRepository _users;
        private readonly MirrorBotsRepository _mirrorBots;
        private readonly IAdminNotifier _notifier;

        public BotCallbackHandler(
            UsersRepository users,
            MirrorBotsRepository mirrorBots,
            IAdminNotifier notifier)
        {
            _users = users;
            _mirrorBots = mirrorBots;
            _notifier = notifier;
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, CallbackQuery cq, CancellationToken ct)
        {
            if (cq.Data is null) return;
            var parsed = CbCodec.TryUnpack(cq.Data);
            if (parsed is null) return;

            var chatId = cq.Message?.Chat.Id ?? cq.From.Id;

            var taskEntity = new TaskEntity()
            {
                botContext = ctx,
                tGclient = client,
                tGchatId = chatId,
                tGcallbackQuery = cq,
            };

            taskEntity = await UpsertSeen(taskEntity, ct);
            if (taskEntity is null) return;

            var cb = parsed.Value;

            // Чтобы "часики" не крутились
            await client.AnswerCallbackQuery(cq.Id, cancellationToken: ct);

            ////MENU
            if (cb.Section.Equals(BotRoutes.Callbacks.Menu._section, StringComparison.OrdinalIgnoreCase))
            {
                switch (cb.Action)
                {

                    case string s when s.Equals(BotRoutes.Callbacks.Menu.MenuMainAction, StringComparison.OrdinalIgnoreCase):
                        {
                            taskEntity.answerText = BotUi.Text.Menu(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.Menu(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Menu.HelpAction, StringComparison.OrdinalIgnoreCase):
                        {
                            taskEntity.answerText = BotUi.Text.Help(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.Help(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Menu.RefAction, StringComparison.OrdinalIgnoreCase):
                        {
                            taskEntity.answerText = BotUi.Text.Ref(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.Ref(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                }
            }

            //LANGUAGE
            if (cb.Section.Equals(BotRoutes.Callbacks.Lang._section, StringComparison.OrdinalIgnoreCase))
            {
                switch (cb.Action)
                {
                    case string s when s.Equals(BotRoutes.Callbacks.Lang.ChooseAction, StringComparison.OrdinalIgnoreCase):
                        {
                            taskEntity.answerText = BotUi.Text.LangChoose(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.LangChoose(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }

                    case string s when s.Equals(BotRoutes.Callbacks.Lang.SetAction, StringComparison.OrdinalIgnoreCase):
                        {
                            var newLang = UiLangExt.ParseOrDefault(cb.Args.ElementAtOrDefault(0), UiLang.Ru);
                            taskEntity.userEntity = await _users.SetPreferredLangAsync(cq.From.Id, newLang, DateTime.UtcNow, ct);

                            taskEntity.answerText = BotUi.Text.LangSet(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.LangChoose(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                }
            }

            //BOTS
            if (cb.Section.Equals(BotRoutes.Callbacks.Bot._section, StringComparison.OrdinalIgnoreCase))
            {
                switch (cb.Action)
                {
                    case string s when s.Equals(BotRoutes.Callbacks.Bot.AddAction, StringComparison.OrdinalIgnoreCase):
                        {
                            taskEntity.answerText = BotUi.Text.BotAdd(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Bot.MyAction, StringComparison.OrdinalIgnoreCase):
                        {
                            var ownerId = cq.From.Id;
                            var bots = await _mirrorBots.GetByOwnerTgIdAsync(ownerId, ct);

                            var items = bots.Select(b => new BotListItem(
                                Id: b.Id.ToString(),
                                Title: "@" + (b.BotUsername ?? "unknown"),
                                IsEnabled: b.IsEnabled)).ToList();

                            var text = items.Count == 0 ? "У вас пока нет ботов." : "Ваши боты:";


                            taskEntity.answerText = BotUi.Text.BotsMy(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.BotsMy(taskEntity, items);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Bot.EditAction, StringComparison.OrdinalIgnoreCase):
                        {
                            if (!TryGetObjectId(cb.Args, 0, out var botId)) return;
                            taskEntity.mirrorBotEntity = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
                            if (taskEntity.mirrorBotEntity is null)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNotFound(taskEntity);
                                taskEntity.answerKbrd = BotUi.Keyboards.BotsMy(taskEntity, null);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }
                            // security check: только владелец
                            //TODO добавить оповещение типа АЛАРМ
                            if (taskEntity.mirrorBotEntity.OwnerTelegramUserId != cq.From.Id)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNoAccess(taskEntity);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }



                            taskEntity.answerText = BotUi.Text.BotEdit(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.BotEdit(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Bot.StopAction, StringComparison.OrdinalIgnoreCase):
                        {
                            if (!TryGetObjectId(cb.Args, 0, out var botId)) return;
                            taskEntity.mirrorBotEntity = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
                            if (taskEntity.mirrorBotEntity is null)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNotFound(taskEntity);
                                taskEntity.answerKbrd = BotUi.Keyboards.BotsMy(taskEntity, null);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }
                            // security check: только владелец
                            //TODO добавить оповещение типа АЛАРМ
                            if (taskEntity.mirrorBotEntity.OwnerTelegramUserId != cq.From.Id)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNoAccess(taskEntity);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }                           

                            var nowUtc = DateTime.UtcNow;
                            taskEntity.mirrorBotEntity = await _mirrorBots.SetEnabledAsync(botId, false, nowUtc, ct);
                            taskEntity.answerText = BotUi.Text.BotEdit(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.BotEdit(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Bot.StartAction, StringComparison.OrdinalIgnoreCase):
                        {
                            if (!TryGetObjectId(cb.Args, 0, out var botId)) return;
                            taskEntity.mirrorBotEntity = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
                            if (taskEntity.mirrorBotEntity is null)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNotFound(taskEntity);
                                taskEntity.answerKbrd = BotUi.Keyboards.BotsMy(taskEntity, null);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }
                            // security check: только владелец
                            //TODO добавить оповещение типа АЛАРМ
                            if (taskEntity.mirrorBotEntity.OwnerTelegramUserId != cq.From.Id)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNoAccess(taskEntity);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }

                            var nowUtc = DateTime.UtcNow;
                            taskEntity.mirrorBotEntity = await _mirrorBots.SetEnabledAsync(botId, true, nowUtc, ct);
                            taskEntity.answerText = BotUi.Text.BotEdit(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.BotEdit(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteAction, StringComparison.OrdinalIgnoreCase):
                        {
                            if (!TryGetObjectId(cb.Args, 0, out var botId)) return;
                            taskEntity.mirrorBotEntity = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
                            if (taskEntity.mirrorBotEntity is null)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNotFound(taskEntity);
                                taskEntity.answerKbrd = BotUi.Keyboards.BotsMy(taskEntity, null);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }
                            // security check: только владелец
                            //TODO добавить оповещение типа АЛАРМ
                            if (taskEntity.mirrorBotEntity.OwnerTelegramUserId != cq.From.Id)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNoAccess(taskEntity);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }

                            taskEntity.answerText = BotUi.Text.BotDeleteConfirm(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.BotDeleteConfirm(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteYesAction, StringComparison.OrdinalIgnoreCase):
                        {
                            if (!TryGetObjectId(cb.Args, 0, out var botId)) return;
                            taskEntity.mirrorBotEntity = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
                            if (taskEntity.mirrorBotEntity is null)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNotFound(taskEntity);
                                taskEntity.answerKbrd = BotUi.Keyboards.BotsMy(taskEntity, null);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }
                            // security check: только владелец
                            //TODO добавить оповещение типа АЛАРМ
                            if (taskEntity.mirrorBotEntity.OwnerTelegramUserId != cq.From.Id)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNoAccess(taskEntity);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }

                            await _mirrorBots.DeleteByOdjectIdAsync(botId, ct);

                            taskEntity.answerText = BotUi.Text.BotDeleteYesResult(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.BotsMy(taskEntity, null);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    case string s when s.Equals(BotRoutes.Callbacks.Bot.DeleteNoAction, StringComparison.OrdinalIgnoreCase):
                        {
                            if (!TryGetObjectId(cb.Args, 0, out var botId)) return;
                            taskEntity.mirrorBotEntity = await _mirrorBots.GetByOdjectIdAsync(botId, ct);
                            if (taskEntity.mirrorBotEntity is null)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNotFound(taskEntity);
                                taskEntity.answerKbrd = BotUi.Keyboards.BotsMy(taskEntity, null);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }
                            // security check: только владелец
                            //TODO добавить оповещение типа АЛАРМ
                            if (taskEntity.mirrorBotEntity.OwnerTelegramUserId != cq.From.Id)
                            {
                                taskEntity.answerText = BotUi.Text.BotEditNoAccess(taskEntity);
                                await SendOrEditAsync(taskEntity, ct);
                                return;
                            }

                            taskEntity.answerText = BotUi.Text.BotEdit(taskEntity);
                            taskEntity.answerKbrd = BotUi.Keyboards.BotEdit(taskEntity);
                            await SendOrEditAsync(taskEntity, ct);
                            return;
                        }
                    default:
                        {
                            return;
                        }

                }
            }          
        }


        private static async Task SendOrEditAsync(TaskEntity entity, CancellationToken ct)
        {
            if (entity is null) return;
            if (entity.tGclient is null) return;
            if (entity.tGchatId is null) return;
            if (entity.answerText is null) return;
            if (entity.tGcallbackQuery is null) return;



            if (entity.tGcallbackQuery.Message is not { } m)
            {
                await entity.tGclient.SendMessage(
                    chatId: entity.tGchatId,
                    text: entity.answerText,
                    replyMarkup: entity.answerKbrd,
                    cancellationToken: ct);
                return;
            }

            try
            {
                await entity.tGclient.EditMessageText(
                    chatId: entity.tGchatId,
                    messageId: m.MessageId,
                    text: entity.answerText,
                    replyMarkup: entity.answerKbrd as InlineKeyboardMarkup,
                    cancellationToken: ct);
            }
            catch (ApiRequestException ex) when (
                ex.Message.Contains("message can't be edited", StringComparison.OrdinalIgnoreCase) ||  // [web:540]
                ex.Message.Contains("message not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))  // [web:565]
            {
                // Если "not modified" — можно вообще ничего не делать, но fallback тоже не критичен.
                // Если "can't be edited"/"not found" — шлём новое сообщение.
                if (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
                    return;

                await entity.tGclient.SendMessage(
                    chatId: m.Chat.Id,
                    text: entity.answerText,
                    replyMarkup: entity.answerKbrd,
                    cancellationToken: ct);
            }
        }


        private static bool TryGetObjectId(string[] args, int index, out ObjectId id)
        {
            id = ObjectId.Empty;
            if (args.Length <= index) return false;
            return ObjectId.TryParse(args[index], out id);
        }

        private async Task<TaskEntity?> UpsertSeen(TaskEntity entity, CancellationToken ct)
        {
            if (entity is null) return entity;
            if (entity.botContext is null) return entity;
            if (entity.tGcallbackQuery is null) return entity;
            var from = entity.tGcallbackQuery.From;
            if (from is null) return entity;

            var nowUtc = DateTime.UtcNow;
            var lastBotKey = entity.botContext.MirrorBotId == ObjectId.Empty ? "__main__" : entity.botContext.MirrorBotId.ToString();


            long? refOwner = null;
            ObjectId? refBotId = null;

            if (entity.botContext.OwnerTelegramUserId != 0 && entity.botContext.MirrorBotId != ObjectId.Empty && from.Id != entity.botContext.OwnerTelegramUserId)
            {
                refOwner = entity.botContext.OwnerTelegramUserId;
                refBotId = entity.botContext.MirrorBotId;
            }

            var seen = new UserSeenEvent(
                TgUserId: from.Id,
                TgUsername: from.Username,
                TgFirstName: from.FirstName,
                TgLastName: from.LastName,
                TgLangCode: from.LanguageCode,
                LastBotKey: lastBotKey,
                LastChatId: entity.tGchatId,
                SeenAtUtc: nowUtc,
                ReferrerOwnerTgUserId: refOwner,
                ReferrerMirrorBotId: refBotId
            );

            _notifier.TryEnqueue(AdminChannel.Info,
               $"#id{seen.TgUserId} @{seen.TgUsername}\n" +
               $"/{entity.tGcallbackQuery.Data}\n" +
               $"@{entity.botContext.BotUsername}");

            entity.userEntity = await _users.UpsertSeenAsync(seen, ct);
            return entity;
        }

        private async Task<UiLang> ResolveLangAsync(CallbackQuery cq, CancellationToken ct)
        {
            // 1) Язык, который пользователь выбрал сам (приоритет)
            var preferred = await _users.GetPreferredLangAsync(cq.From.Id, ct);
            if (preferred is not UiLang.Def)
                return preferred;

            // 2) Фолбэк: язык Telegram клиента (IETF language tag, может быть "ru" или "ru-RU") [web:81]
            var lc = cq.From.LanguageCode; // string? (может быть null) [web:81]
            if (!string.IsNullOrWhiteSpace(lc))
            {
                var normalized = lc.Trim().ToLowerInvariant();

                // берём базовый язык из тега: "ru-RU" -> "ru"
                var baseCode = normalized.Split('-', '_')[0];

                return baseCode switch
                {
                    "ru" => UiLang.Ru,
                    "en" => UiLang.En,
                    _ => UiLang.Ru
                };
            }

            // 3) Дефолт
            return UiLang.Ru;
        }
    }
}

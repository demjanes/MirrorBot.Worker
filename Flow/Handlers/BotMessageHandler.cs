using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Entities;
using MirrorBot.Worker.Data.Events;
using MirrorBot.Worker.Data.Repo;
using MirrorBot.Worker.Flow.Routes;
using MirrorBot.Worker.Flow.UI;
using MirrorBot.Worker.Services.AdminNotifierService;
using MongoDB.Bson;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MirrorBot.Worker.Flow.Handlers
{
    public sealed class BotMessageHandler
    {
        private readonly UsersRepository _users;
        private readonly MirrorBotsRepository _mirrorBots;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAdminNotifier _notifier;

        public BotMessageHandler(
            UsersRepository users,
            MirrorBotsRepository mirrorBots,
            IHttpClientFactory httpClientFactory,
            IAdminNotifier notifier)
        {
            _users = users;
            _mirrorBots = mirrorBots;
            _httpClientFactory = httpClientFactory;
            _notifier = notifier;
        }

        public async Task HandleAsync(BotContext ctx, ITelegramBotClient client, Message msg, CancellationToken ct)
        {
            if (msg.From is null) return;
            if (msg.Text is null) return;

            var chatId = msg.Chat.Id;

            var taskEntity = new TaskEntity()
            {
                botContext = ctx,
                tGclient = client,
                tGchatId = chatId,
                tGmessage = msg,
                tGuserText = msg.Text,
            };

            taskEntity = await UpsertSeen(taskEntity, ct);
            if (taskEntity is null) return;


            switch (taskEntity.tGuserText)
            {
                case BotRoutes.Commands.Start:
                    {
                        taskEntity.answerText = BotUi.Text.Start(taskEntity);
                        taskEntity.answerKbrd = BotUi.Keyboards.StartR(taskEntity);
                        await SendAsync(taskEntity, ct);
                        return;
                    }
                case BotRoutes.Commands.HideKbrdTxt_Ru:
                case BotRoutes.Commands.HideKbrdTxt_En:
                    {
                        taskEntity.answerText = BotUi.Text.HideKbrd(taskEntity);
                        taskEntity.answerKbrd = new ReplyKeyboardRemove();
                        await SendAsync(taskEntity, ct);
                        return;
                    }
                case BotRoutes.Commands.HelpTxt_Ru:
                case BotRoutes.Commands.HelpTxt_En:
                case BotRoutes.Commands.Help:
                    {
                        taskEntity.answerText = BotUi.Text.Help(taskEntity);
                        taskEntity.answerKbrd = BotUi.Keyboards.Help(taskEntity);
                        await SendAsync(taskEntity, ct);
                        return;
                    }
                case BotRoutes.Commands.MenuTxt_Ru:
                case BotRoutes.Commands.MenuTxt_En:
                case BotRoutes.Commands.Menu:
                    {
                        taskEntity.answerText = BotUi.Text.Menu(taskEntity);
                        taskEntity.answerKbrd = BotUi.Keyboards.Menu(taskEntity);
                        await SendAsync(taskEntity, ct);
                        return;
                    }
                case BotRoutes.Commands.Ref:
                    {
                        taskEntity.answerText = BotUi.Text.Ref(taskEntity);
                        taskEntity.answerKbrd = BotUi.Keyboards.Ref(taskEntity);
                        await SendAsync(taskEntity, ct);
                        return;
                    }
                default:
                    {
                        // 2) ввод токена (как раньше)
                        if (LooksLikeToken(msg.Text))
                        {
                            await TryAddMirrorBotByTokenAsync(client, msg, ct);
                            return;
                        }

                        //неизвестная команда
                        taskEntity.answerText = BotUi.Text.Unknown(taskEntity);
                        taskEntity.answerKbrd = BotUi.Keyboards.StartR(taskEntity);
                        await SendAsync(taskEntity, ct);
                        return;
                    }
            }

        }

        private static async Task SendAsync(TaskEntity entity, CancellationToken ct)
        {
            if (entity is null) return;
            if (entity.tGclient is null) return;
            if (entity.tGchatId is null) return;
            if (entity.answerText is null) return;

            await entity.tGclient.SendMessage(
                chatId: entity.tGchatId,
                text: entity.answerText,
                replyMarkup: entity.answerKbrd,
                cancellationToken: ct);
        }


        private async Task<TaskEntity?> UpsertSeen(TaskEntity entity, CancellationToken ct)
        {
            if (entity is null) return entity;
            if (entity.botContext is null) return entity;
            if (entity.tGmessage is null) return entity;
            var from = entity.tGmessage.From;
            if (from is null) return entity;

            var nowUtc = DateTime.UtcNow;
            var lastBotKey = entity.botContext.MirrorBotId == ObjectId.Empty ? "__main__" : entity.botContext.MirrorBotId.ToString();

            long? refOwner = null;
            ObjectId? refBotId = null;

            // реферал только для зеркал + нельзя сам себе
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
                $"{entity.tGmessage.Text}\n" +
                $"@{entity.botContext.BotUsername}");

            entity.userEntity = await _users.UpsertSeenAsync(seen, ct);
            return entity;
        }

        // --- Internals ---
        private async Task TryAddMirrorBotByTokenAsync(ITelegramBotClient client, Message msg, CancellationToken ct)
        {
            var token = msg.Text!;
            var existing = await _mirrorBots.GetByTokenAsync(token, ct);
            if (existing is not null)
            {
                await client.SendMessage(msg.Chat.Id, BotUi.Text.TokenAlreadyAdded, cancellationToken: ct);
                return;
            }

            var http = _httpClientFactory.CreateClient("telegram");
            var probe = new TelegramBotClient(new TelegramBotClientOptions(token), http);
            var me = await probe.GetMe(ct);


            var mirror = new MirrorBotEntity
            {
                OwnerTelegramUserId = msg.From!.Id,
                Token = token,
                BotUsername = me.Username,
                IsEnabled = true
            };

            await _mirrorBots.InsertAsync(mirror, ct);

            var chatId = msg.Chat.Id;
            var taskEntity = new TaskEntity() { 
                tGclient = client,
                tGchatId = chatId,
                mirrorBotEntity = mirror 
            };
            taskEntity.answerText = BotUi.Text.BotAddResult(taskEntity);
            taskEntity.answerKbrd = BotUi.Keyboards.BotAddResult(taskEntity);
            await SendAsync(taskEntity, ct);
        }
        private static bool LooksLikeToken(string text)
            => text.Contains(':') && text.Length >= 20;
    }
}

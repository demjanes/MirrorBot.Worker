using MirrorBot.Worker.Bot;
using MirrorBot.Worker.Data.Repo;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace MirrorBot.Worker.Services
{
    public sealed class BroadcastService
    {
        private readonly UsersRepository _users;
        private readonly IBotClientResolver _bots;
        private readonly ILogger<BroadcastService> _log;

        public BroadcastService(
            UsersRepository users,
            IBotClientResolver bots,
            ILogger<BroadcastService> log)
        {
            _users = users;
            _bots = bots;
            _log = log;
        }

        public async Task SendToLastActiveBotAsync(string text, TimeSpan activeWithin, CancellationToken ct)
        {
            var activeAfterUtc = DateTime.UtcNow - activeWithin;

            // Разбиваем текст на части один раз
            var messageParts = MessageSplitter.Split(text);

            await foreach (var user in _users.StreamForBroadcastAsync(activeAfterUtc, ct))
            {
                if (user.LastBotKey is null || user.LastChatId is null)
                    continue;
                if (!_bots.TryGetClient(user.LastBotKey, out var client))
                    continue; // бот сейчас не запущен

                try
                {
                    // В Bot API отправка идёт по chat_id, поэтому мы храним LastChatId
                    // Отправляем все части сообщения каждому пользователю
                    foreach (var messagePart in messageParts)
                    {
                        await client.SendMessage(
                            chatId: user.LastChatId.Value,
                            text: messagePart,
                            cancellationToken: ct);
                    }
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 403)
                {
                    // типичный кейс: пользователь заблокировал бота
                    await _users.MarkCantSendLastBotAsync(
                        telegramUserId: user.TgUserId,
                        reason: ex.Message,
                        ct: ct);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Broadcast failed. user={UserId} botKey={BotKey}", user.TgUserId, user.LastBotKey);
                }
            }
        }
    }
}

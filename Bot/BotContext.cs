using MongoDB.Bson;

namespace MirrorBot.Worker.Bot
{
    public sealed record BotContext(
        ObjectId MirrorBotId,
        long OwnerTelegramUserId,
        string Token,
        string? BotUsername
    );
}

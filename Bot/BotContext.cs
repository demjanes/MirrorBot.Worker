using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Bot
{
    public sealed record BotContext(
        ObjectId MirrorBotId,
        long OwnerTelegramUserId,
        string Token,
        string? BotUsername
    );
}

using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Events
{
    public sealed record UserSeenEvent(
    long TgUserId,
    string? TgUsername,
    string? TgFirstName,
    string? TgLastName,
    string? TgLangCode,
    string? LastBotKey,
    long? LastChatId,
    DateTime SeenAtUtc,
    long? ReferrerOwnerTgUserId,
    ObjectId? ReferrerMirrorBotId
);
}

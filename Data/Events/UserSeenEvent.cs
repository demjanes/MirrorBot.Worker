using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Events
{
    /// <summary>
    /// Событие: пользователь был замечен (написал сообщение).
    /// </summary>
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



    //public record UserSeenEvent(
    //    long TgUserId,
    //    string? TgUsername,
    //    string? TgFirstName,
    //    string? TgLastName,
    //    string? TgLangCode,
    //    string LastBotKey,
    //    long? LastChatId,
    //    DateTime SeenAtUtc
    //// ✅ УБРАНО: ReferrerOwnerTgUserId и ReferrerMirrorBotId
    //);
}

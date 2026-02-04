using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data
{
    public sealed class UserEntity
    {
        [BsonId] 
        public ObjectId Id { get; set; }

        public long TelegramUserId { get; set; }
        public string? Username { get; set; }
        public DateTime FirstSeenAtUtc { get; set; } = DateTime.UtcNow;

        public long? ReferrerOwnerTelegramUserId { get; set; }
        public ObjectId? ReferrerMirrorBotId { get; set; }
    }
}

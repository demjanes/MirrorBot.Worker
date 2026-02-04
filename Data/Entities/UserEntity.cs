using MirrorBot.Worker.Data.Repo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Entities
{
    public sealed class UserEntity : BaseRepository
    {        
        public long TgUserId { get; set; }
        public string? TgUsername { get; set; }       
        public string? TgFirstName { get; set; }       
        public string? TgLastName { get; set; }


        public string? LastBotKey { get; set; }      // например "__main__" или mirrorBotId.ToString()
        public long? LastChatId { get; set; }        // сохраняй msg.Chat.Id
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastMessageAtUtc { get; set; }
        public bool CanSendLastBot { get; set; } = true;
        public string? LastSendError { get; set; }


        public long? ReferrerOwnerTgUserId { get; set; }
        public ObjectId? ReferrerMirrorBotId { get; set; }
    }
}

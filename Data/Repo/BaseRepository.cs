using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MirrorBot.Worker.Data.Repo
{
    public abstract class BaseRepository
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

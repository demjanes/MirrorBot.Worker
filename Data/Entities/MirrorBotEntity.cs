using MirrorBot.Worker.Data.Repo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Data.Entities
{
    public sealed class MirrorBotEntity : BaseRepository
    {      
        public long OwnerTelegramUserId { get; set; }

        public string Token { get; set; } = default!; // по твоему требованию plain text

        public string? BotUsername { get; set; }

        public bool IsEnabled { get; set; } = true;

        
        public DateTime? LastSeenAtUtc { get; set; }
        public string? LastError { get; set; }
    }
}

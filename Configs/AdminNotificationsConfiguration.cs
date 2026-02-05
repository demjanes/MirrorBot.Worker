using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Configs
{
    public sealed class AdminNotificationsConfiguration
    {
        public bool Enabled { get; set; } = true;

        public ChannelOptions Info { get; set; } = new();
        public ChannelOptions Ref { get; set; } = new();

        public int BatchSize { get; set; } = 1;
        public int FlushIntervalMs { get; set; } = 1000;
        public bool CombineIntoSingleMessage { get; set; } = true;
        public int MaxQueueSize { get; set; } = 10_000;

        public sealed class ChannelOptions
        {
            public bool Enabled { get; set; } = true;
            public long ChatId { get; set; } // -100...
        }
    }
}

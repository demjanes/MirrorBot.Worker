using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Configs
{
    public sealed class LimitsConfiguration
    {
        public int MaxBotsPerUser { get; init; } = 10;
    }
}

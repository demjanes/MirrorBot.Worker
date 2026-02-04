using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Flow
{
    public sealed class CommandRouter
    {
        public static string? TryGetCommand(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!text.StartsWith('/')) return null;
            return text.Split(' ', '\n', '\t')[0].Trim();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Flow.UI.Models
{
    public sealed record BotListItem(
       string Id,
       string Title,
       bool IsEnabled);
}

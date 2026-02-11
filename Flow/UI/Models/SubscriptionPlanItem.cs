using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Flow.UI.Models
{
    public record SubscriptionPlanItem(
      string Id,
      string Name,
      decimal PriceRub,
      int DurationDays,
      bool IsCurrentPlan);
}

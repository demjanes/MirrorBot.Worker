using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Payments.Models
{
    public class AmountDto
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "RUB";
    }
}

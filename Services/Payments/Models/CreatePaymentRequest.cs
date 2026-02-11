using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Payments.Models
{
    /// <summary>
    /// Запрос на создание платежа в ЮКассе.
    /// </summary>
    public class CreatePaymentRequest
    {
        [JsonPropertyName("amount")]
        public AmountDto Amount { get; set; } = new();

        [JsonPropertyName("confirmation")]
        public ConfirmationDto Confirmation { get; set; } = new();

        [JsonPropertyName("capture")]
        public bool Capture { get; set; } = true;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Payments.Models
{
    /// <summary>
    /// Ответ от ЮКассы при создании платежа.
    /// </summary>
    public class CreatePaymentResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public AmountDto Amount { get; set; } = new();

        [JsonPropertyName("confirmation")]
        public ConfirmationDto? Confirmation { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("paid")]
        public bool Paid { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}

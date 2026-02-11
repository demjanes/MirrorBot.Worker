using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Payments.Models
{


    /// <summary>
    /// Webhook уведомление от ЮКассы.
    /// </summary>
    public class YooKassaWebhook
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public CreatePaymentResponse Object { get; set; } = new();
    }

}

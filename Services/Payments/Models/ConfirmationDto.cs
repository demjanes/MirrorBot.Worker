using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.Payments.Models
{
    public class ConfirmationDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "redirect";

        [JsonPropertyName("return_url")]
        public string? ReturnUrl { get; set; }

        [JsonPropertyName("confirmation_url")]
        public string? ConfirmationUrl { get; set; }
    }
}

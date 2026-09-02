using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms06.Contracts;

public sealed class CallbackReceiptDto
{
        [JsonPropertyName("callbackId")]
        [Required]
        public string CallbackId { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("paymentIntentId")]
        public string? PaymentIntentId { get; set; }
}

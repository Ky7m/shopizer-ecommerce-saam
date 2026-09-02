using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class PaymentCommandResponseDto
{
        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("commandId")]
        [Required]
        public string CommandId { get; set; }

        [JsonPropertyName("action")]
        [Required]
        public string Action { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("paymentReference")]
        public string? PaymentReference { get; set; }
}

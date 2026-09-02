using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms06.Contracts;

public sealed class RefundDto
{
        [JsonPropertyName("refundId")]
        [Required]
        public string RefundId { get; set; }

        [JsonPropertyName("paymentIntentId")]
        [Required]
        public string PaymentIntentId { get; set; }

        [JsonPropertyName("amount")]
        [Required]
        public string Amount { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("providerReference")]
        public string? ProviderReference { get; set; }

        [JsonPropertyName("requestedAt")]
        [Required]
        public string RequestedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public string? CompletedAt { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
}

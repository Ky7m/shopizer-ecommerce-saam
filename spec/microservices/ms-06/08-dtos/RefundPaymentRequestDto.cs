using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms06.Contracts;

public sealed class RefundPaymentRequestDto
{
        [JsonPropertyName("amount")]
        [Required]
        [RegularExpression(@"^[0-9]+\.[0-9]{2,4}$")]
        public string Amount { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }

        [JsonPropertyName("reason")]
        [MaxLength(255)]
        public string? Reason { get; set; }
}

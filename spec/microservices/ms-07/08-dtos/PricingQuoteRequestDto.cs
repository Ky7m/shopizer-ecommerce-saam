using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class PricingQuoteRequestDto
{
        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }

        [JsonPropertyName("items")]
        public List<PricingItemRequestDto> Items { get; set; } = new();

        [JsonPropertyName("promoCode")]
        [MaxLength(160)]
        public string? PromoCode { get; set; }

        [JsonPropertyName("evaluationAt")]
        public string? EvaluationAt { get; set; }
}

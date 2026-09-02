using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class PromotionEvaluationResponseDto
{
        [JsonPropertyName("promoCode")]
        [Required]
        public string PromoCode { get; set; }

        [JsonPropertyName("matched")]
        public bool Matched { get; set; }

        [JsonPropertyName("discountRate")]
        public decimal? DiscountRate { get; set; }

        [JsonPropertyName("reduction")]
        [Range(0, double.MaxValue)]
        public decimal Reduction { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }

        [JsonPropertyName("items")]
        public List<PromotionItemResultDto> Items { get; set; } = new();

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
}

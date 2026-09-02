using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class PricingQuoteResponseDto
{
        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }

        [JsonPropertyName("items")]
        public List<PricedItemDto> Items { get; set; } = new();

        [JsonPropertyName("additionalPriceLines")]
        public List<AdditionalPriceLineDto> AdditionalPriceLines { get; set; } = new();

        [JsonPropertyName("merchandiseSubtotal")]
        [Range(0, double.MaxValue)]
        public decimal MerchandiseSubtotal { get; set; }

        [JsonPropertyName("promotion")]
        public PricingQuotePromotionDto Promotion { get; set; }

        [JsonPropertyName("subtotalAfterPromotion")]
        [Range(0, double.MaxValue)]
        public decimal SubtotalAfterPromotion { get; set; }

        [JsonPropertyName("downstreamComponents")]
        public List<string> DownstreamComponents { get; set; } = new();

        [JsonPropertyName("grandTotalOwnedBy")]
        [Required]
        public string GrandTotalOwnedBy { get; set; }
}

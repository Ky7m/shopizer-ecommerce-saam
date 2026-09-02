using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class PromotionItemResultDto
{
        [JsonPropertyName("productSku")]
        [Required]
        [StringLength(160, MinimumLength = 1)]
        public string ProductSku { get; set; }

        [JsonPropertyName("variantSku")]
        [StringLength(160, MinimumLength = 1)]
        public string? VariantSku { get; set; }

        [JsonPropertyName("quantity")]
        [Range(1, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("effectiveUnitPrice")]
        [Range(0, double.MaxValue)]
        public decimal EffectiveUnitPrice { get; set; }

        [JsonPropertyName("reduction")]
        [Range(0, double.MaxValue)]
        public decimal Reduction { get; set; }
}

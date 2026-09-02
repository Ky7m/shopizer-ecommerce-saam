using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class PricingItemRequestDto
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

        [JsonPropertyName("attributes")]
        public List<PricingAttributeDto> Attributes { get; set; } = new();
}

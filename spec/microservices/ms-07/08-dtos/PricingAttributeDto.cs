using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class PricingAttributeDto
{
        [JsonPropertyName("attributeId")]
        [Required]
        [StringLength(160, MinimumLength = 1)]
        public string AttributeId { get; set; }

        [JsonPropertyName("valueId")]
        [Required]
        [StringLength(160, MinimumLength = 1)]
        public string ValueId { get; set; }

        [JsonPropertyName("priceAdjustment")]
        [Range(0, double.MaxValue)]
        public decimal PriceAdjustment { get; set; }
}

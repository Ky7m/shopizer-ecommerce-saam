using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class PricedItemDto
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

    [JsonPropertyName("unitPrice")]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("lineSubtotal")]
    [Range(0, double.MaxValue)]
    public decimal LineSubtotal { get; set; }

    [JsonPropertyName("additionalPrices")]
    public List<AdditionalPriceLineDto> AdditionalPrices { get; set; } = new();
}

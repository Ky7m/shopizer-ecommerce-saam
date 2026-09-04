using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class ProductPriceCalculationResponseDto
{
    [JsonPropertyName("productSku")]
    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string ProductSku { get; set; }

    [JsonPropertyName("selectedVariantSku")]
    [StringLength(160, MinimumLength = 1)]
    public string? SelectedVariantSku { get; set; }

    [JsonPropertyName("availabilitySource")]
    [Required]
    public string AvailabilitySource { get; set; }

    [JsonPropertyName("currency")]
    [Required]
    [RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; }

    [JsonPropertyName("originalPrice")]
    [Range(0, double.MaxValue)]
    public decimal OriginalPrice { get; set; }

    [JsonPropertyName("finalPrice")]
    [Range(0, double.MaxValue)]
    public decimal FinalPrice { get; set; }

    [JsonPropertyName("discounted")]
    public bool Discounted { get; set; }

    [JsonPropertyName("discountedPrice")]
    public decimal? DiscountedPrice { get; set; }

    [JsonPropertyName("discountPercent")]
    [Range(0, double.MaxValue)]
    public int DiscountPercent { get; set; }

    [JsonPropertyName("discountEndDate")]
    public string? DiscountEndDate { get; set; }

    [JsonPropertyName("attributeAdjustment")]
    public decimal? AttributeAdjustment { get; set; }

    [JsonPropertyName("customerPricingApplied")]
    public bool CustomerPricingApplied { get; set; }

    [JsonPropertyName("pricingBasis")]
    [Required]
    public string PricingBasis { get; set; }

    [JsonPropertyName("additionalPrices")]
    public List<AdditionalPriceLineDto> AdditionalPrices { get; set; } = new();
}

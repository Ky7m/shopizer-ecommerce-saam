using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class PriceDto
{
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }

    [JsonPropertyName("legacyPriceId")]
    public long? LegacyPriceId { get; set; }

    [JsonPropertyName("priceListId")]
    [Required]
    public string PriceListId { get; set; }

    [JsonPropertyName("productSku")]
    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string ProductSku { get; set; }

    [JsonPropertyName("variantSku")]
    [StringLength(160, MinimumLength = 1)]
    public string? VariantSku { get; set; }

    [JsonPropertyName("availabilityId")]
    public long? AvailabilityId { get; set; }

    [JsonPropertyName("code")]
    [Required]
    [RegularExpression(@"^[A-Za-z0-9_]+$")]
    public string Code { get; set; }

    [JsonPropertyName("amount")]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [JsonPropertyName("priceType")]
    [Required]
    public string PriceType { get; set; }

    [JsonPropertyName("defaultPrice")]
    public bool DefaultPrice { get; set; }

    [JsonPropertyName("specialStartDate")]
    public string? SpecialStartDate { get; set; }

    [JsonPropertyName("specialEndDate")]
    public string? SpecialEndDate { get; set; }

    [JsonPropertyName("specialAmount")]
    public decimal? SpecialAmount { get; set; }

    [JsonPropertyName("productIdentifierId")]
    public long? ProductIdentifierId { get; set; }

    [JsonPropertyName("discounted")]
    public bool Discounted { get; set; }

    [JsonPropertyName("price")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [JsonPropertyName("discountedPrice")]
    public decimal? DiscountedPrice { get; set; }

    [JsonPropertyName("discountPercent")]
    [Range(0, double.MaxValue)]
    public int DiscountPercent { get; set; }

    [JsonPropertyName("discountEndDate")]
    public string? DiscountEndDate { get; set; }

    [JsonPropertyName("currency")]
    [RegularExpression(@"^[A-Z]{3}$")]
    public string? Currency { get; set; }
}

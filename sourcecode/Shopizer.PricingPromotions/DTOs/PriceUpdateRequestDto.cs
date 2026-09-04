using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class PriceUpdateRequestDto
{
    [JsonPropertyName("code")]
    [Required]
    [StringLength(80, MinimumLength = 1)]
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
}

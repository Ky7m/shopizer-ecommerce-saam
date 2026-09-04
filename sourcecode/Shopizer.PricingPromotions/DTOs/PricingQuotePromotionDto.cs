using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class PricingQuotePromotionDto
{
    [JsonPropertyName("promoCode")]
    [Required]
    public string PromoCode { get; set; }

    [JsonPropertyName("matched")]
    public bool Matched { get; set; }

    [JsonPropertyName("reduction")]
    [Range(0, double.MaxValue)]
    public decimal Reduction { get; set; }
}

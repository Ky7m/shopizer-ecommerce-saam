using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class PromotionEvaluationRequestDto
{
    [JsonPropertyName("promoCode")]
    [Required]
    [StringLength(160, MinimumLength = 0)]
    public string PromoCode { get; set; }

    [JsonPropertyName("items")]
    public List<PricingItemRequestDto> Items { get; set; } = new();

    [JsonPropertyName("evaluationAt")]
    public string? EvaluationAt { get; set; }
}

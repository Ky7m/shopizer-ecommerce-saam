using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class PriceListResponseDto
{
    [JsonPropertyName("items")]
    public List<PriceDto> Items { get; set; } = new();
}

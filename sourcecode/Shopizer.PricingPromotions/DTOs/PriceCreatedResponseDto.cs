using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class PriceCreatedResponseDto
{
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }

    [JsonPropertyName("legacyPriceId")]
    public long? LegacyPriceId { get; set; }

    [JsonPropertyName("productSku")]
    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string ProductSku { get; set; }

    [JsonPropertyName("availabilityId")]
    [Range(1, double.MaxValue)]
    public long AvailabilityId { get; set; }
}

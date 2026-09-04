using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class InactiveProcessorDto
{
    [JsonPropertyName("code")]
    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("reason")]
    [Required]
    public string Reason { get; set; }
}

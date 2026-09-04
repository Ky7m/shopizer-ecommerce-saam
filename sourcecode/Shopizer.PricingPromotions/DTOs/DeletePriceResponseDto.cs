using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class DeletePriceResponseDto
{
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Shipping.DTOs;

public sealed class ShippingCountryDto
{
    [JsonPropertyName("code")]
    [Required]
    [RegularExpression(@"^[A-Z]{2}$")]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }
}

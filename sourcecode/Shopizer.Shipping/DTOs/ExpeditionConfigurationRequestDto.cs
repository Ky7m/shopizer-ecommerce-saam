using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Shipping.DTOs;

public sealed class ExpeditionConfigurationRequestDto
{
    [JsonPropertyName("internationalShipping")]
    [Required]
    public bool? InternationalShipping { get; set; }

    [JsonPropertyName("taxOnShipping")]
    [Required]
    public bool? TaxOnShipping { get; set; }

    [JsonPropertyName("shipToCountry")]
    [Required]
    public List<string>? ShipToCountry { get; set; }
}

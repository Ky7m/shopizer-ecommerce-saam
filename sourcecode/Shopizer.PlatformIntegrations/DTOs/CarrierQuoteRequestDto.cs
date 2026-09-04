using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class CarrierQuoteRequestDto
{
    [JsonPropertyName("environment")]
    [Required]
    public string Environment { get; set; }

    [JsonPropertyName("origin")]
    public AddressDto Origin { get; set; }

    [JsonPropertyName("destination")]
    public AddressDto Destination { get; set; }

    [JsonPropertyName("packages")]
    public List<PackageDto> Packages { get; set; } = new();

    [JsonPropertyName("orderTotal")]
    public decimal? OrderTotal { get; set; }
}

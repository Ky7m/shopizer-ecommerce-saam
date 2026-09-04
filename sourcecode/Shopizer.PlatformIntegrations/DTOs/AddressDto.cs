using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class AddressDto
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zoneCode")]
    public string? ZoneCode { get; set; }

    [JsonPropertyName("countryCode")]
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; }

    [JsonPropertyName("postalCode")]
    [Required]
    [MinLength(1)]
    public string PostalCode { get; set; }
}

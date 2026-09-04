using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class AddressDto
{
    [JsonPropertyName("streetAddress")]
    public string? StreetAddress { get; set; }

    [JsonPropertyName("city")]
    [Required]
    public string City { get; set; }

    [JsonPropertyName("postalCode")]
    [Required]
    public string PostalCode { get; set; }

    [JsonPropertyName("countryCode")]
    [Required]
    public string CountryCode { get; set; }

    [JsonPropertyName("stateProvince")]
    public string? StateProvince { get; set; }

    [JsonPropertyName("zoneCode")]
    public string? ZoneCode { get; set; }
}

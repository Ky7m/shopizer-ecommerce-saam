using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Shipping.DTOs;

public sealed class ShippingAddressRequestDto
{
    [JsonPropertyName("countryCode")]
    [Required]
    [RegularExpression(@"^[A-Z]{2}$")]
    public string CountryCode { get; set; }

    [JsonPropertyName("postalCode")]
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string PostalCode { get; set; }

    [JsonPropertyName("address")]
    [MaxLength(256)]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    [MaxLength(100)]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    [MaxLength(100)]
    public string? State { get; set; }

    [JsonPropertyName("zoneCode")]
    [MaxLength(32)]
    public string? ZoneCode { get; set; }
}

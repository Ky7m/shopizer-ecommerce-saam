using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Shipping.DTOs;

public sealed class ShippingOriginRequestDto
{
    [JsonPropertyName("address")]
    [Required]
    [MaxLength(256)]
    public string Address { get; set; }

    [JsonPropertyName("city")]
    [Required]
    [MaxLength(100)]
    public string City { get; set; }

    [JsonPropertyName("postalCode")]
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; }

    [JsonPropertyName("state")]
    [MaxLength(100)]
    public string? State { get; set; }

    [JsonPropertyName("countryCode")]
    [Required]
    [RegularExpression(@"^[A-Z]{2}$")]
    public string CountryCode { get; set; }

    [JsonPropertyName("zoneCode")]
    [MaxLength(32)]
    public string? ZoneCode { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class ShippingAddressRequestDto
{
    [JsonPropertyName("postalCode")]
    [Required]
    public string PostalCode { get; set; }

    [JsonPropertyName("countryCode")]
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; }
}

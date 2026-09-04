using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class AddressUpdateRequestDto
{
    [JsonPropertyName("billing")]
    public AddressDto? Billing { get; set; }

    [JsonPropertyName("delivery")]
    public AddressDto? Delivery { get; set; }
}

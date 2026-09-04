using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class DistanceRequestDto
{
    [JsonPropertyName("origin")]
    public AddressDto Origin { get; set; }

    [JsonPropertyName("destination")]
    public AddressDto Destination { get; set; }

    [JsonPropertyName("allowedZoneCodes")]
    public List<string> AllowedZoneCodes { get; set; } = new();
}

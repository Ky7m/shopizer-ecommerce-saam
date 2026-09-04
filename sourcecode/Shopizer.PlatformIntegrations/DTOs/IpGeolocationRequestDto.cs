using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class IpGeolocationRequestDto
{
    [JsonPropertyName("ipAddress")]
    public object IpAddress { get; set; }
}

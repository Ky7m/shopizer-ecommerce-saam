using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class CarrierQuoteResponseDto
{
    [JsonPropertyName("provider")]
    [Required]
    public string Provider { get; set; }

    [JsonPropertyName("requestType")]
    [Required]
    public string RequestType { get; set; }

    [JsonPropertyName("packageSize")]
    public string? PackageSize { get; set; }

    [JsonPropertyName("options")]
    public List<CarrierOptionDto> Options { get; set; } = new();

    [JsonPropertyName("suppressedReason")]
    public string? SuppressedReason { get; set; }
}

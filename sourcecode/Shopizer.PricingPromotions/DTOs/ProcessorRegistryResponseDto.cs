using System.Text.Json.Serialization;

namespace Shopizer.PricingPromotions.DTOs;

public sealed class ProcessorRegistryResponseDto
{
    [JsonPropertyName("processors")]
    public List<ProcessorDto> Processors { get; set; } = new();

    [JsonPropertyName("inactive")]
    public List<InactiveProcessorDto> Inactive { get; set; } = new();
}

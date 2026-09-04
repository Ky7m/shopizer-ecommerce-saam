using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class DistanceResponseDto
{
    [JsonPropertyName("enriched")]
    public bool Enriched { get; set; }

    [JsonPropertyName("destination")]
    public CoordinateDto? Destination { get; set; }

    [JsonPropertyName("distanceKm")]
    public decimal? DistanceKm { get; set; }

    [JsonPropertyName("suppressedReason")]
    public string? SuppressedReason { get; set; }
}

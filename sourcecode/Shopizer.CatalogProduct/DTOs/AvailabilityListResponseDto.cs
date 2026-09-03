using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class AvailabilityListResponseDto
{
        [JsonPropertyName("items")]
        public List<AvailabilityDto> Items { get; set; } = new();
}

using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class AvailabilityListResponseDto
{
        [JsonPropertyName("items")]
        public List<AvailabilityDto> Items { get; set; } = new();
}

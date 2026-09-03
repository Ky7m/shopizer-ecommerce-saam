using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class ReplaceAvailabilityRequestDto
{
        [JsonPropertyName("items")]
        public List<AvailabilityInputDto> Items { get; set; } = new();
}

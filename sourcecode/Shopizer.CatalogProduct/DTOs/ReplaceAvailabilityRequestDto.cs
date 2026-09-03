using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class ReplaceAvailabilityRequestDto
{
        [JsonPropertyName("items")]
        public List<AvailabilityInputDto> Items { get; set; } = new();
}

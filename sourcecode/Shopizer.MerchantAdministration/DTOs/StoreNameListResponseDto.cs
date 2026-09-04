using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class StoreNameListResponseDto
{
    [JsonPropertyName("items")]
    public List<StoreNameDto> Items { get; set; } = new();
}

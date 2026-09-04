using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class OrderHistoryResponseDto
{
    [JsonPropertyName("items")]
    public List<OrderHistoryEntryDto> Items { get; set; } = new();
}

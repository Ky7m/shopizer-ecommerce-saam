using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class OrderListResponseDto
{
    [JsonPropertyName("items")]
    public List<OrderDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}

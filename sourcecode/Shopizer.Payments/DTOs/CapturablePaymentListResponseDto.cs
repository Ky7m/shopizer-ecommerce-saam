using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class CapturablePaymentListResponseDto
{
    [JsonPropertyName("items")]
    public List<CapturablePaymentDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}

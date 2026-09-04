using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class PaymentMethodListResponseDto
{
    [JsonPropertyName("items")]
    public List<PaymentMethodDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}

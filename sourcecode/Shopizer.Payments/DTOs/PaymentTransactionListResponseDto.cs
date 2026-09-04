using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class PaymentTransactionListResponseDto
{
    [JsonPropertyName("items")]
    public List<PaymentTransactionDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}

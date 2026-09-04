using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class PaymentTransactionListResponseDto
{
        [JsonPropertyName("items")]
        public List<PaymentTransactionDto> Items { get; set; } = new();
}

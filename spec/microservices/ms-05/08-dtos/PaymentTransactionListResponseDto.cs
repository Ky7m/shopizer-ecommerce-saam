using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class PaymentTransactionListResponseDto
{
        [JsonPropertyName("items")]
        public List<PaymentTransactionDto> Items { get; set; } = new();
}

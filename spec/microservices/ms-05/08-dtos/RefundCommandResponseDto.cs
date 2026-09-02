using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class RefundCommandResponseDto
{
        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("refundId")]
        [Required]
        public string RefundId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("remainingRefundable")]
        public decimal? RemainingRefundable { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }
}

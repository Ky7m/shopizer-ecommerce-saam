using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class PaymentTransactionDto
{
        [JsonPropertyName("transactionId")]
        [Required]
        public string TransactionId { get; set; }

        [JsonPropertyName("action")]
        [Required]
        public string Action { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        public string Currency { get; set; }

        [JsonPropertyName("paymentReference")]
        public string? PaymentReference { get; set; }

        [JsonPropertyName("occurredAt")]
        [Required]
        public string OccurredAt { get; set; }
}

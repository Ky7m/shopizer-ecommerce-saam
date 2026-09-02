using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms06.Contracts;

public sealed class PaymentOperationDto
{
        [JsonPropertyName("paymentOperationId")]
        [Required]
        public string PaymentOperationId { get; set; }

        [JsonPropertyName("paymentIntentId")]
        [Required]
        public string PaymentIntentId { get; set; }

        [JsonPropertyName("operationType")]
        [Required]
        public string OperationType { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("requestedAmount")]
        [Required]
        public string RequestedAmount { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }

        [JsonPropertyName("idempotencyKey")]
        [Required]
        public string IdempotencyKey { get; set; }

        [JsonPropertyName("providerAttemptId")]
        public string? ProviderAttemptId { get; set; }

        [JsonPropertyName("providerReference")]
        public string? ProviderReference { get; set; }

        [JsonPropertyName("failureCode")]
        public string? FailureCode { get; set; }

        [JsonPropertyName("failureMessage")]
        public string? FailureMessage { get; set; }

        [JsonPropertyName("createdAt")]
        [Required]
        public string CreatedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public string? CompletedAt { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class PaymentTransactionDto
{
    [JsonPropertyName("paymentTransactionId")]
    [Required]
    public string PaymentTransactionId { get; set; }

    [JsonPropertyName("paymentIntentId")]
    [Required]
    public string PaymentIntentId { get; set; }

    [JsonPropertyName("paymentOperationId")]
    public string? PaymentOperationId { get; set; }

    [JsonPropertyName("operationType")]
    [Required]
    public string OperationType { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }

    [JsonPropertyName("amount")]
    [Required]
    public string Amount { get; set; }

    [JsonPropertyName("currency")]
    [Required]
    [RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; }

    [JsonPropertyName("providerCode")]
    [Required]
    public string ProviderCode { get; set; }

    [JsonPropertyName("providerReference")]
    public string? ProviderReference { get; set; }

    [JsonPropertyName("providerStatus")]
    public string? ProviderStatus { get; set; }

    [JsonPropertyName("providerCorrelationId")]
    public string? ProviderCorrelationId { get; set; }

    [JsonPropertyName("providerDetails")]
    public Dictionary<string, object?>? ProviderDetails { get; set; } = new();

    [JsonPropertyName("occurredAt")]
    [Required]
    public string OccurredAt { get; set; }

    [JsonPropertyName("sequenceNo")]
    public long SequenceNo { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}

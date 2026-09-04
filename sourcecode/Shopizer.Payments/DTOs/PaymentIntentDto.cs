using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class PaymentIntentDto
{
    [JsonPropertyName("paymentIntentId")]
    [Required]
    public string PaymentIntentId { get; set; }

    [JsonPropertyName("tenantId")]
    [Required]
    public string TenantId { get; set; }

    [JsonPropertyName("storeId")]
    [Required]
    public string StoreId { get; set; }

    [JsonPropertyName("checkoutSessionId")]
    [Required]
    public string CheckoutSessionId { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("providerCode")]
    [Required]
    public string ProviderCode { get; set; }

    [JsonPropertyName("providerConfigVersion")]
    public long? ProviderConfigVersion { get; set; }

    [JsonPropertyName("amount")]
    [Required]
    public string Amount { get; set; }

    [JsonPropertyName("currency")]
    [Required]
    [RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }

    [JsonPropertyName("authorizedAmount")]
    public string? AuthorizedAmount { get; set; }

    [JsonPropertyName("capturedAmount")]
    public string? CapturedAmount { get; set; }

    [JsonPropertyName("refundableAmount")]
    public string? RefundableAmount { get; set; }

    [JsonPropertyName("clientSecretReference")]
    public string? ClientSecretReference { get; set; }

    [JsonPropertyName("createdAt")]
    [Required]
    public string CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    [Required]
    public string UpdatedAt { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}

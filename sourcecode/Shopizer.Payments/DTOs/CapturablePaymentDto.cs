using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class CapturablePaymentDto
{
    [JsonPropertyName("paymentIntentId")]
    [Required]
    public string PaymentIntentId { get; set; }

    [JsonPropertyName("orderId")]
    [Required]
    public string OrderId { get; set; }

    [JsonPropertyName("amount")]
    [Required]
    public string Amount { get; set; }

    [JsonPropertyName("currency")]
    [Required]
    public string Currency { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }

    [JsonPropertyName("authorizedAt")]
    [Required]
    public string AuthorizedAt { get; set; }

    [JsonPropertyName("providerCode")]
    public string? ProviderCode { get; set; }
}

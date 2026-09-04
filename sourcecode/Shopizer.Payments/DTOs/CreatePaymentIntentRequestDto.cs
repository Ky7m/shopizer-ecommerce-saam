using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class CreatePaymentIntentRequestDto
{
    [JsonPropertyName("checkoutSessionId")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string CheckoutSessionId { get; set; }

    [JsonPropertyName("orderId")]
    [MaxLength(100)]
    public string? OrderId { get; set; }

    [JsonPropertyName("paymentMethodCode")]
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string PaymentMethodCode { get; set; }

    [JsonPropertyName("amount")]
    [Required]
    [RegularExpression(@"^[0-9]+\.[0-9]{2,4}$")]
    public string Amount { get; set; }

    [JsonPropertyName("currency")]
    [Required]
    [RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; }

    [JsonPropertyName("paymentToken")]
    public string? PaymentToken { get; set; }

    [JsonPropertyName("amountSnapshotVersion")]
    public long? AmountSnapshotVersion { get; set; }
}

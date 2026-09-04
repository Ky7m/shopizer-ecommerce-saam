using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class PaymentRequestDto
{
    [JsonPropertyName("amount")]
    [Required]
    [RegularExpression(@"^[0-9]+(\.[0-9]{1,4})?$")]
    public string Amount { get; set; }

    [JsonPropertyName("paymentModule")]
    [Required]
    public string PaymentModule { get; set; }

    [JsonPropertyName("paymentType")]
    [Required]
    public string PaymentType { get; set; }

    [JsonPropertyName("transactionType")]
    [Required]
    public string TransactionType { get; set; }

    [JsonPropertyName("paymentToken")]
    [Required]
    [MinLength(1)]
    public string PaymentToken { get; set; }
}

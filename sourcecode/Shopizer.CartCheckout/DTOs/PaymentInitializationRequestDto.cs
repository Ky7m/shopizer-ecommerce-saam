using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class PaymentInitializationRequestDto
{
    [JsonPropertyName("amount")]
    [Required]
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
    public string PaymentToken { get; set; }
}

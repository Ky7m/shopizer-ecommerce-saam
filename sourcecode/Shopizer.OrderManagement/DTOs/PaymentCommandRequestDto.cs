using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class PaymentCommandRequestDto
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; }

    [JsonPropertyName("paymentReference")]
    public string? PaymentReference { get; set; }
}

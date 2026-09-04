using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Payments.DTOs;

public sealed class CapturePaymentRequestDto
{
    [JsonPropertyName("amount")]
    [Required]
    [RegularExpression(@"^[0-9]+\.[0-9]{2,4}$")]
    public string Amount { get; set; }

    [JsonPropertyName("currency")]
    [Required]
    [RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; }
}

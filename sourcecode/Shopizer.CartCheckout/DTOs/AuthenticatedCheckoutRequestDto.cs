using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class AuthenticatedCheckoutRequestDto
{
    [JsonPropertyName("currency")]
    [Required]
    [RegularExpression(@"^[A-Z]{3}$")]
    public string Currency { get; set; }

    [JsonPropertyName("shippingQuoteId")]
    public string? ShippingQuoteId { get; set; }

    [JsonPropertyName("payment")]
    public PaymentRequestDto Payment { get; set; }

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    [JsonPropertyName("customerAgreement")]
    public bool CustomerAgreement { get; set; }
}

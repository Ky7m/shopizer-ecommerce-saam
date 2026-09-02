using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms06.Contracts;

public sealed class AuthorizePaymentRequestDto
{
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

        [JsonPropertyName("payerReference")]
        public string? PayerReference { get; set; }

        [JsonPropertyName("providerIntentReference")]
        public string? ProviderIntentReference { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object?>? Metadata { get; set; } = new();
}

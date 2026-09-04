using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class PaymentInitializationResponseDto
{
        [JsonPropertyName("submissionId")]
        [Required]
        public string SubmissionId { get; set; }

        [JsonPropertyName("paymentState")]
        [Required]
        public string PaymentState { get; set; }

        [JsonPropertyName("providerReference")]
        public string? ProviderReference { get; set; }

        [JsonPropertyName("amount")]
        [Required]
        public string Amount { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }
}

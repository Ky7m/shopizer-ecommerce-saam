using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CheckoutSubmissionResponseDto
{
        [JsonPropertyName("submissionId")]
        [Required]
        public string SubmissionId { get; set; }

        [JsonPropertyName("checkoutSessionId")]
        public string? CheckoutSessionId { get; set; }

        [JsonPropertyName("state")]
        [Required]
        public string State { get; set; }

        [JsonPropertyName("amount")]
        public string? Amount { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("eventId")]
        public string? EventId { get; set; }

        [JsonPropertyName("downstream")]
        public Dictionary<string, object?>? Downstream { get; set; } = new();
}

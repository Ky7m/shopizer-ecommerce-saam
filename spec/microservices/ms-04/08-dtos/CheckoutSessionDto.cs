using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CheckoutSessionDto
{
        [JsonPropertyName("checkoutSessionId")]
        [Required]
        public string CheckoutSessionId { get; set; }

        [JsonPropertyName("cartId")]
        public long CartId { get; set; }

        [JsonPropertyName("customerId")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        [JsonPropertyName("storeId")]
        public string? StoreId { get; set; }

        [JsonPropertyName("state")]
        [Required]
        public string State { get; set; }

        [JsonPropertyName("cartVersion")]
        public long? CartVersion { get; set; }

        [JsonPropertyName("currencyCode")]
        [Required]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("expiresAt")]
        [Required]
        public string ExpiresAt { get; set; }

        [JsonPropertyName("submittedAt")]
        public string? SubmittedAt { get; set; }

        [JsonPropertyName("failureCode")]
        public string? FailureCode { get; set; }
}

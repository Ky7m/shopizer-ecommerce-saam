using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CartQuoteReferenceDto
{
        [JsonPropertyName("quoteReferenceId")]
        [Required]
        public string QuoteReferenceId { get; set; }

        [JsonPropertyName("tenantId")]
        [Required]
        public string TenantId { get; set; }

        [JsonPropertyName("storeId")]
        [Required]
        public string StoreId { get; set; }

        [JsonPropertyName("checkoutSessionId")]
        public string? CheckoutSessionId { get; set; }

        [JsonPropertyName("cartId")]
        public long CartId { get; set; }

        [JsonPropertyName("quoteKind")]
        [Required]
        public string QuoteKind { get; set; }

        [JsonPropertyName("providerQuoteReference")]
        [Required]
        public string ProviderQuoteReference { get; set; }

        [JsonPropertyName("providerVersion")]
        public string? ProviderVersion { get; set; }

        [JsonPropertyName("expiresAt")]
        [Required]
        public string ExpiresAt { get; set; }
}

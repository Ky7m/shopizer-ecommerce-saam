using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class CheckoutIdempotencyKeyDto
{
        [JsonPropertyName("idempotencyRecordId")]
        [Required]
        public string IdempotencyRecordId { get; set; }

        [JsonPropertyName("tenantId")]
        [Required]
        public string TenantId { get; set; }

        [JsonPropertyName("storeId")]
        [Required]
        public string StoreId { get; set; }

        [JsonPropertyName("customerId")]
        public long? CustomerId { get; set; }

        [JsonPropertyName("cartId")]
        public long CartId { get; set; }

        [JsonPropertyName("operation")]
        [Required]
        public string Operation { get; set; }

        [JsonPropertyName("idempotencyKey")]
        [Required]
        public string IdempotencyKey { get; set; }

        [JsonPropertyName("requestHash")]
        [Required]
        public string RequestHash { get; set; }

        [JsonPropertyName("state")]
        [Required]
        public string State { get; set; }

        [JsonPropertyName("originalStatus")]
        public int? OriginalStatus { get; set; }

        [JsonPropertyName("originalResponse")]
        public Dictionary<string, object?>? OriginalResponse { get; set; } = new();
}

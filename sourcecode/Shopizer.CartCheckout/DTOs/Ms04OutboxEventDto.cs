using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CartCheckout.DTOs;

public sealed class Ms04OutboxEventDto
{
        [JsonPropertyName("eventId")]
        [Required]
        public string EventId { get; set; }

        [JsonPropertyName("tenantId")]
        [Required]
        public string TenantId { get; set; }

        [JsonPropertyName("storeId")]
        [Required]
        public string StoreId { get; set; }

        [JsonPropertyName("aggregateId")]
        [Required]
        public string AggregateId { get; set; }

        [JsonPropertyName("eventType")]
        [Required]
        public string EventType { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, object?> Payload { get; set; } = new();

        [JsonPropertyName("state")]
        [Required]
        public string State { get; set; }

        [JsonPropertyName("attemptCount")]
        public int AttemptCount { get; set; }

        [JsonPropertyName("occurredAt")]
        [Required]
        public string OccurredAt { get; set; }

        [JsonPropertyName("publishedAt")]
        public string? PublishedAt { get; set; }
}

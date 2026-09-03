using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms03.Contracts;

public sealed class EventMetadataDto
{
        [JsonPropertyName("eventId")]
        [Required]
        public string EventId { get; set; }

        [JsonPropertyName("eventType")]
        [Required]
        public string EventType { get; set; }

        [JsonPropertyName("eventVersion")]
        [Range(1, double.MaxValue)]
        public int EventVersion { get; set; }

        [JsonPropertyName("occurredAt")]
        [Required]
        public string OccurredAt { get; set; }

        [JsonPropertyName("tenantId")]
        [Required]
        public string TenantId { get; set; }

        [JsonPropertyName("storeId")]
        [Required]
        public string StoreId { get; set; }

        [JsonPropertyName("correlationId")]
        [Required]
        public string CorrelationId { get; set; }
}

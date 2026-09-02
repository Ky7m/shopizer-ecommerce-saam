using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class OutboxEventDto
{
        [JsonPropertyName("eventId")]
        [Required]
        public string EventId { get; set; }

        [JsonPropertyName("operationId")]
        [Required]
        public string OperationId { get; set; }

        [JsonPropertyName("eventType")]
        [Required]
        public string EventType { get; set; }

        [JsonPropertyName("aggregateType")]
        [Required]
        public string AggregateType { get; set; }

        [JsonPropertyName("aggregateId")]
        [Required]
        public string AggregateId { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, object?> Payload { get; set; } = new();

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("availableAt")]
        [Required]
        public string AvailableAt { get; set; }

        [JsonPropertyName("publishedAt")]
        public string? PublishedAt { get; set; }
}

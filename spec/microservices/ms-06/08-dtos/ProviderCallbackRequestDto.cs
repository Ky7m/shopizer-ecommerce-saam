using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms06.Contracts;

public sealed class ProviderCallbackRequestDto
{
        [JsonPropertyName("eventId")]
        public string? EventId { get; set; }

        [JsonPropertyName("providerReference")]
        public string? ProviderReference { get; set; }

        [JsonPropertyName("eventType")]
        [Required]
        public string EventType { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, object?> Payload { get; set; } = new();
}

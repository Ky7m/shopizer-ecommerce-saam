using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class AdapterDto
{
        [JsonPropertyName("endpointId")]
        [Required]
        public string EndpointId { get; set; }

        [JsonPropertyName("integrationType")]
        public IntegrationTypeDto IntegrationType { get; set; }

        [JsonPropertyName("provider")]
        [Required]
        public string Provider { get; set; }

        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("environment")]
        [Required]
        public string Environment { get; set; }

        [JsonPropertyName("status")]
        public AdapterStatusDto Status { get; set; }

        [JsonPropertyName("configurationRef")]
        [Required]
        public string ConfigurationRef { get; set; }

        [JsonPropertyName("endpointUri")]
        public string? EndpointUri { get; set; }

        [JsonPropertyName("capabilities")]
        public Dictionary<string, object?> Capabilities { get; set; } = new();

        [JsonPropertyName("supplementalConfiguration")]
        public Dictionary<string, object?>? SupplementalConfiguration { get; set; } = new();

        [JsonPropertyName("timeoutMs")]
        [Range(100, 120000)]
        public int TimeoutMs { get; set; }

        [JsonPropertyName("maxAttempts")]
        [Range(1, 10)]
        public int MaxAttempts { get; set; }
}

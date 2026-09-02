using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class RefreshAdapterRequestDto
{
        [JsonPropertyName("moduleType")]
        public IntegrationTypeDto ModuleType { get; set; }

        [JsonPropertyName("code")]
        [Required]
        [MinLength(1)]
        public string Code { get; set; }

        [JsonPropertyName("provider")]
        [Required]
        [MinLength(1)]
        public string Provider { get; set; }

        [JsonPropertyName("environment")]
        [Required]
        [MinLength(1)]
        public string Environment { get; set; }

        [JsonPropertyName("configurationRef")]
        [Required]
        [MinLength(1)]
        public string ConfigurationRef { get; set; }

        [JsonPropertyName("resolvedEndpointUri")]
        public string? ResolvedEndpointUri { get; set; }

        [JsonPropertyName("capabilities")]
        public Dictionary<string, object?>? Capabilities { get; set; } = new();

        [JsonPropertyName("timeoutMs")]
        public int? TimeoutMs { get; set; }

        [JsonPropertyName("maxAttempts")]
        public int? MaxAttempts { get; set; }

        [JsonPropertyName("config1")]
        public string? Config1 { get; set; }

        [JsonPropertyName("config2")]
        public string? Config2 { get; set; }

        [JsonPropertyName("credentials")]
        public Dictionary<string, object?>? Credentials { get; set; } = new();

        [JsonPropertyName("packageTypes")]
        public List<string>? PackageTypes { get; set; } = new();
}

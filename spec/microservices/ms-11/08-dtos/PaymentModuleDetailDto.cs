using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class PaymentModuleDetailDto
{
        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("configurable")]
        public string? Configurable { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("configured")]
        public bool? Configured { get; set; }

        [JsonPropertyName("defaultSelected")]
        public bool DefaultSelected { get; set; }

        [JsonPropertyName("requiredKeys")]
        public List<string> RequiredKeys { get; set; } = new();

        [JsonPropertyName("integrationKeys")]
        public Dictionary<string, object?> IntegrationKeys { get; set; } = new();

        [JsonPropertyName("integrationOptions")]
        public Dictionary<string, object?> IntegrationOptions { get; set; } = new();

        [JsonPropertyName("environment")]
        [Required]
        public string Environment { get; set; }

        [JsonPropertyName("secretsPresent")]
        public bool SecretsPresent { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }
}

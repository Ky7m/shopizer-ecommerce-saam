using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms09.Contracts;

public sealed class ShippingModuleConfigurationRequestDto
{
        [JsonPropertyName("moduleCode")]
        [Required]
        [MinLength(1)]
        public string ModuleCode { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("defaultSelected")]
        public bool DefaultSelected { get; set; }

        [JsonPropertyName("environment")]
        [Required]
        public string Environment { get; set; }

        [JsonPropertyName("integrationKeys")]
        public Dictionary<string, object?>? IntegrationKeys { get; set; } = new();

        [JsonPropertyName("integrationOptions")]
        public Dictionary<string, object?>? IntegrationOptions { get; set; } = new();
}

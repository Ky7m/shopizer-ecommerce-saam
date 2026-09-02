using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms06.Contracts;

public sealed class PaymentMethodDto
{
        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("providerCode")]
        [Required]
        public string ProviderCode { get; set; }

        [JsonPropertyName("eligible")]
        public bool Eligible { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("defaultSelected")]
        public bool DefaultSelected { get; set; }

        [JsonPropertyName("configurable")]
        public bool? Configurable { get; set; }

        [JsonPropertyName("environment")]
        [Required]
        public string Environment { get; set; }

        [JsonPropertyName("configurationVersion")]
        public long? ConfigurationVersion { get; set; }

        [JsonPropertyName("publicConfiguration")]
        public Dictionary<string, object?>? PublicConfiguration { get; set; } = new();
}

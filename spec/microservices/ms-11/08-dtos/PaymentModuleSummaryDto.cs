using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class PaymentModuleSummaryDto
{
        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("configured")]
        public bool Configured { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("binaryImage")]
        public string? BinaryImage { get; set; }

        [JsonPropertyName("requiredKeys")]
        public List<string> RequiredKeys { get; set; } = new();

        [JsonPropertyName("configurable")]
        [Required]
        public string Configurable { get; set; }
}

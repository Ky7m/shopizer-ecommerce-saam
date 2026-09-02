using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class CarrierOptionDto
{
        [JsonPropertyName("provider")]
        [Required]
        public string Provider { get; set; }

        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("estimatedDays")]
        public string? EstimatedDays { get; set; }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class MerchantConfigurationRecordRequestDto
{
        [JsonPropertyName("type")]
        public MerchantConfigurationTypeDto Type { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("value")]
        public Dictionary<string, object?>? Value { get; set; } = new();
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class ModuleReplacementResponseDto
{
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("replaced")]
        public bool Replaced { get; set; }

        [JsonPropertyName("cacheInvalidated")]
        public bool CacheInvalidated { get; set; }
}
